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

    public DocXDocumentParser(string path, DialogueProcessor dialogueProcessor) {
        DialogueProcessor = dialogueProcessor;
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

    public DialogueProcessor DialogueProcessor { get; }
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
        return index < 0 || index >= _doc.Lists.Count ?
            string.Empty :
            _doc.Lists[index].Items.FirstOrDefault()?.Text ?? string.Empty;
    }

    public List<DialogueTopic> ParseDialogue(int index) {
        var branches = new List<DialogueTopic>();
        var list = _doc.Lists[index];

        if (list.Items.Count == 0) return branches;

        //Evaluate if the player starts dialogue
        if (IsPlayerLine(list.Items[0])) {
            branches.AddRange(
                list.Items.Where(p => p.IndentLevel == FirstIndentationLevel)
                    .Select(
                        x => {
                            var currentBranchInfo = AddTopicInfo(x);
                            DialogueProcessor.PreProcess(currentBranchInfo);
                            return new DialogueTopic { TopicInfos = [currentBranchInfo] };
                        }));
        } else {
            //One new branch, NPC starts to talk
            var currentTopicInfo = new DialogueTopicInfo();
            var currentBranch = new DialogueTopic { TopicInfos = [currentTopicInfo] };
            branches.Add(currentBranch);

            AddLinksAndResponses(list.Items[0], currentTopicInfo);
            DialogueProcessor.PreProcess(currentTopicInfo);
        }

        return branches;
    }

    public List<DialogueTopic> ParseOneLiner(int index) {
        var topicInfos = _doc.Lists[index]
            .Items
            .Where(p => p.IndentLevel == FirstIndentationLevel)
            .Select(
                p => {
                    var topic = new DialogueTopicInfo { Responses = { DialogueResponse.Build(GetFormattedText(p)) } };
                    DialogueProcessor.PreProcess(topic);
                    return topic;
                })
            .ToList();

        return [new DialogueTopic { TopicInfos = topicInfos }];
    }

    public List<DialogueTopic> ParseScene(int index) {
        return ParseOneLiner(index);
    }

    private DialogueTopicInfo AddTopicInfo(Paragraph paragraph) {
        var topicInfo = new DialogueTopicInfo();
        var startingIndentation = paragraph.IndentLevel;

        topicInfo.Prompt = paragraph.Text;

        paragraph = paragraph.NextParagraph;
        if (paragraph.IndentLevel == startingIndentation + 1) AddLinksAndResponses(paragraph, topicInfo);

        return topicInfo;
    }

    private void AddLinksAndResponses(Paragraph paragraph, DialogueTopicInfo topicInfo) {
        var startingIndentation = paragraph.IndentLevel;

        //Add further responses
        while (paragraph is not null && paragraph.IndentLevel == startingIndentation) {
            topicInfo.Responses.Add(DialogueResponse.Build(GetFormattedText(paragraph)));

            paragraph = paragraph.NextParagraph;
            while (paragraph is { IndentLevel: null } && paragraph.Xml != paragraph.NextParagraph.Xml)
                paragraph = paragraph.NextParagraph;
        }

        //Add links
        while (paragraph is not null && paragraph.IndentLevel > startingIndentation) {
            if (paragraph.IndentLevel == startingIndentation + 1) {
                var nextTopicInfo = AddTopicInfo(paragraph);
                topicInfo.Links.Add(new DialogueTopic { TopicInfos = [nextTopicInfo] });
                DialogueProcessor.PreProcess(nextTopicInfo);
            }

            paragraph = paragraph.NextParagraph;
        }
    }

    private bool IsPlayerLine(Paragraph paragraph) {
        return paragraph.MagicText.NotNull().All(magicText => magicText.formatting?.Bold is not (null or false));
    }

    private IEnumerable<FormattedText> GetFormattedText(Paragraph paragraph) {
        return paragraph.MagicText
            .NotNull()
            .Select(
                text => new FormattedText(
                    text.text,
                    text.formatting?.Bold ?? false,
                    text.formatting?.FontColor ?? Color.Black))
            .ToList();
    }

    private FormattedText GetFormattedText(Xceed.Document.NET.FormattedText text) {
        return new FormattedText(text.text, text.formatting.Bold ?? false, text.formatting.FontColor ?? Color.Black);
    }
}
