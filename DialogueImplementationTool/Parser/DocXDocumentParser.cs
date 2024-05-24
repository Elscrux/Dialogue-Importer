using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Xceed.Document.NET;
using Xceed.Words.NET;
namespace DialogueImplementationTool.Parser;

public sealed class DocXDocumentParser : ReactiveObject, IDocumentParser {
    private const int FirstIndentationLevel = 0;
    private readonly DocX _doc;

    public DocXDocumentParser(string path) {
        FilePath = path;

        var tryLoading = true;
        while (tryLoading)
            try {
                _doc = DocX.Load(path);
                tryLoading = false;
            } catch (Exception e) {
                switch (MessageBox.Show(e.Message)) {
                    case MessageBoxResult.None:
                    case MessageBoxResult.Cancel:
                    case MessageBoxResult.No:
                        tryLoading = false;
                        break;
                    case MessageBoxResult.OK:
                    case MessageBoxResult.Yes:
                        break;
                    default: throw new InvalidOperationException();
                }
            }

        _doc ??= DocX.Create(Stream.Null);
        LastIndex = _doc.Lists.Count - 1;
    }

    public string FilePath { get; }
    [Reactive] public int Index { get; set; }
    public int LastIndex { get; }

    public void BacktrackMany() {
        (this as IDocumentIterator).Previous();
    }

    public void SkipMany() {
        (this as IDocumentIterator).Next();
    }

    public string Preview(int index) {
        return index < 0 || index >= _doc.Lists.Count ? string.Empty : _doc.Lists[index].Items.FirstOrDefault()?.Text ?? string.Empty;
    }

    public List<DialogueTopic> ParseDialogue(IDialogueProcessor processor, int index) {
        var branches = new List<DialogueTopic>();
        var list = _doc.Lists[index];
        var paragraphs = EnumerateBetween(list.Items[0], list.Items[^1]);

        //Evaluate if the player starts dialogue
        if (IsPlayerLine(list.Items[0])) {
            var listIndices = list.Items
                .Select((p, i) => p.IndentLevel == FirstIndentationLevel ? i : -1)
                .Where(i => i != -1)
                .Append(list.Items.Count - 1)
                .ToList();

            for (var i = 0; i < listIndices.Count - 1; i++) {
                var firstIndex = listIndices[i];
                var lastIndex = listIndices[i + 1];

                var enumerable = EnumerateBetween(list.Items[firstIndex], list.Items[lastIndex]);
                using var enumerator = enumerable.GetEnumerator();
                if (!enumerator.MoveNext()) continue;

                var topic = GetTopic(processor, enumerator);
                branches.Add(topic);
            }
        } else {
            //One new branch, NPC starts to talk
            using var enumerator = paragraphs.GetEnumerator();
            if (!enumerator.MoveNext()) return branches;

            var topicInfos = GetTopicInfos(processor, enumerator).ToList();
            var currentBranch = new DialogueTopic { TopicInfos = topicInfos };
            branches.Add(currentBranch);
        }

        return branches;

        static IEnumerable<Paragraph> EnumerateBetween(Paragraph first, Paragraph last) {
            var current = first;
            while (current.Xml != last.Xml && current.Xml != current.NextParagraph.Xml) {
                yield return current;

                current = current.NextParagraph;
            }

            yield return last;
        }
    }

    public List<DialogueTopic> ParseOneLiner(IDialogueProcessor processor, int index) {
        var topicInfos = _doc.Lists[index]
            .Items
            .Where(p => p.IndentLevel == FirstIndentationLevel)
            .Select(
                p => {
                    var topicInfo = new DialogueTopicInfo();
                    topicInfo.Responses.Add(processor.BuildResponse(GetFormattedText(p)));
                    processor.Process(topicInfo);
                    return topicInfo;
                })
            .ToList();

        return [new DialogueTopic { TopicInfos = topicInfos }];
    }

    private DialogueTopic GetTopic(IDialogueProcessor processor, IEnumerator<Paragraph> enumerator) {
        var startingIndentation = enumerator.Current.IndentLevel;
        var prompt = enumerator.Current.Text;

        enumerator.MoveNext();
        if (enumerator.Current.IndentLevel != startingIndentation + 1) {
            return new DialogueTopic { TopicInfos = [new DialogueTopicInfo { Prompt = prompt }] };
        }

        var list = new List<DialogueTopicInfo>();
        foreach (var topicInfo in GetTopicInfos(processor, enumerator)) {
            topicInfo.Prompt = prompt;
            list.Add(topicInfo);
        }

        return new DialogueTopic { TopicInfos = list };
    }

    private IEnumerable<DialogueTopicInfo> GetTopicInfos(IDialogueProcessor processor, IEnumerator<Paragraph> enumerator) {
        var startingIndentation = enumerator.Current.IndentLevel;
        var abort = false;

        while (!abort && enumerator.Current.IndentLevel == startingIndentation) {
            var topicInfo = new DialogueTopicInfo();

            //Add responses
            var currentIndentation = startingIndentation;
            while (enumerator.Current.IndentLevel is null || IsValidResponse(enumerator.Current, currentIndentation)) {
                topicInfo.Responses.Add(processor.BuildResponse(GetFormattedText(enumerator.Current)));

                if (enumerator.Current.IndentLevel is not null) currentIndentation = enumerator.Current.IndentLevel;
                if (!enumerator.MoveNext()) {
                    abort = true;
                    break;
                }
            }

            if (abort) {
                processor.Process(topicInfo);
                yield return topicInfo;

                break;
            }

            //Add links
            while (enumerator.Current.IndentLevel is null || enumerator.Current.IndentLevel > startingIndentation) {
                if (IsPlayerLine(enumerator.Current)) {
                    var nextTopic = GetTopic(processor, enumerator);
                    topicInfo.Links.Add(nextTopic);
                } else {
                    if (!enumerator.MoveNext()) {
                        abort = true;
                        break;
                    }
                }
            }

            processor.Process(topicInfo);
            yield return topicInfo;
        }

        bool IsValidResponse(Paragraph newParagraph, int? currentIndentation) {
            // No indent level means it's text between list entries, we just include them for good measure
            // List entries that are not player lines and in the current indentation scope are also included
            return newParagraph.IndentLevel is null
                   || newParagraph.IndentLevel >= currentIndentation && !IsPlayerLine(newParagraph);
        }
    }

    private static bool IsPlayerLine(Paragraph paragraph) {
        return paragraph.MagicText.NotNull().All(magicText => magicText.formatting?.Bold is not (null or false));
    }

    private static List<FormattedText> GetFormattedText(Paragraph paragraph) {
        return paragraph.MagicText
            .NotNull()
            .Select(text => new FormattedText(
                text.text,
                text.formatting?.Bold ?? false,
                text.formatting?.FontColor ?? Color.Black))
            .ToList();
    }
}
