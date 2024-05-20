using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using AODL.Document.Content;
using AODL.Document.Content.Text;
using AODL.Document.TextDocuments;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.Parser;

public sealed class OpenDocumentTextParser : ReactiveObject, IDocumentParser {
    private readonly TextDocument _doc = new();

    public OpenDocumentTextParser(string filePath) {
        FilePath = filePath;
        var tryLoading = true;
        while (tryLoading)
            try {
                _doc.Load(filePath);
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

        MergeLists();

        LastIndex = 0;
        for (var i = _doc.Content.Count - 1; i >= 0; i--) {
            if (string.IsNullOrWhiteSpace(Preview(i))) continue;

            LastIndex = i;
            break;
        }
    }

    public string FilePath { get; }
    [Reactive] public int Index { get; set; }
    public int LastIndex { get; }

    public void BacktrackMany() {
        if (Index == 0) return;

        for (var i = Index - 1; i >= 0; i--) {
            if (_doc.Content[i] is not List) continue;

            Index = i;
            return;
        }

        Index = 0;
    }

    public void SkipMany() {
        if (Index >= _doc.Content.Count - 1) return;

        for (var i = Index + 1; i < _doc.Content.Count; i++) {
            if (_doc.Content[i] is not List) continue;

            Index = i;
            return;
        }

        Index = LastIndex;
    }

    public string Preview(int index) {
        switch (_doc.Content[index]) {
            case List list:
                while (list.Content.Count > 0)
                    if (list.Content[0] is ListItem listItem) {
                        if (listItem.Content.Count > 0)
                            switch (listItem.Content[0]) {
                                case Paragraph paragraph: return GetText(paragraph);
                                case List newList:
                                    list = newList;
                                    break;
                                default: return string.Empty;
                            }
                        else
                            return string.Empty;
                    } else {
                        return string.Empty;
                    }

                break;
            case Paragraph paragraph: return GetText(paragraph);
        }

        return string.Empty;
    }

    public List<DialogueTopic> ParseDialogue(IDialogueProcessor processor, int index) {
        if (_doc.Content[index] is not List list) return [];

        var branches = new List<DialogueTopic>();
        //Evaluate if the player starts dialogue
        var playerDialogue = true;
        if (list.Content.Count > 0) {
            if (list.Content[0] is ListItem listItem) {
                if (listItem.Content.Count > 0) {
                    if (listItem.Content[0] is Paragraph paragraph)
                        playerDialogue = IsPlayerLine(paragraph);
                    else
                        Console.WriteLine(
                            $"Warning: Didn't recognize {listItem.Content[0].GetType()} as paragraph type");
                }
            } else {
                Console.WriteLine($"Warning: Didn't recognize {list.Content[0].GetType()} as list item type");
            }
        }

        if (playerDialogue) {
            foreach (IContent branch in list.Content) {
                if (branch is not ListItem branchItem) continue;

                //Player dialogue - every entry is a new branch
                var currentBranch = AddTopicInfo(processor, branchItem);
                processor.PreProcess(currentBranch);
                branches.Add(new DialogueTopic { TopicInfos = [currentBranch] });
            }
        } else {
            //One new branch, NPC starts to talk
            var currentBranch = new DialogueTopicInfo();
            branches.Add(new DialogueTopic { TopicInfos = [currentBranch] });

            AddLinksAndResponses(processor, list, currentBranch);
            processor.PreProcess(currentBranch);
        }

        return branches;
    }

    public List<DialogueTopic> ParseOneLiner(IDialogueProcessor processor, int index) {
        if (_doc.Content[index] is not List list) return [];

        var topics = new List<DialogueTopicInfo>();
        foreach (IContent listContent in list.Content) {
            if (listContent is not ListItem listItem) continue;

            foreach (IContent itemContent in listItem.Content) {
                switch (itemContent) {
                    case Paragraph paragraph:
                        //Set player text
                        var topicInfo = new DialogueTopicInfo();
                        topicInfo.Responses.Add(processor.BuildResponse(GetFormattedText(paragraph)));
                        processor.PreProcess(topicInfo);
                        topics.Add(topicInfo);
                        break;
                    default:
                        Console.WriteLine($"Warning: Didn't recognize {listContent.GetType()} as response type");
                        break;
                }
            }
        }

        return [new DialogueTopic { TopicInfos = topics }];
    }

    private void MergeLists() {
        var startingIndex = 0;
        while (startingIndex < _doc.Content.Count) {
            if (_doc.Content[startingIndex] is not List currentList) {
                startingIndex++;
                continue;
            }

            var addNextList = false;
            var index = startingIndex + 1;
            var listAdded = ListAdded();

            if (!listAdded) startingIndex++;

            bool ListAdded() {
                var added = false;
                while (index < _doc.Content.Count)
                    switch (_doc.Content[index]) {
                        case Paragraph paragraph:
                            if (paragraph.TextContent.Count == 0) {
                                addNextList = true;
                                _doc.Content.RemoveAt(index);
                            } else {
                                return added;
                            }

                            break;
                        case List list:
                            if (addNextList) {
                                foreach (IContent content in list.Content) {
                                    currentList.Content.Add(content);
                                }

                                _doc.Content.RemoveAt(index);
                                added = true;
                                addNextList = false;
                            } else {
                                return added;
                            }

                            break;
                        default: return added;
                    }

                return added;
            }
        }
    }

    private DialogueTopicInfo AddTopicInfo(IDialogueProcessor processor, IContentContainer listItem) {
        var topic = new DialogueTopicInfo();

        foreach (IContent itemContent in listItem.Content) {
            switch (itemContent) {
                case Paragraph paragraph:
                    //Set player text
                    topic.Prompt = GetText(paragraph);

                    break;
                case List linksAndResponsesList:
                    //Add links and responses
                    AddLinksAndResponses(processor, linksAndResponsesList, topic);

                    break;
                default:
                    Console.WriteLine($"Warning: Didn't recognize {itemContent.GetType()} as topic type");

                    break;
            }
        }

        return topic;
    }

    private void AddLinksAndResponses(
        IDialogueProcessor processor,
        IContentContainer list,
        DialogueTopicInfo topicInfo) {
        foreach (IContent listContent in list.Content) {
            if (listContent is not ListItem listItem) continue;

            foreach (IContent topicContent in listItem.Content) {
                switch (topicContent) {
                    case Paragraph paragraph:
                        //Add responses
                        topicInfo.Responses.Add(processor.BuildResponse(GetFormattedText(paragraph)));
                        break;
                    case List linkList:
                        //Add links
                        foreach (IContent linkContent in linkList.Content) {
                            if (linkContent is not ListItem linkItem) continue;

                            var nextTopicInfo = AddTopicInfo(processor, linkItem);
                            var nextTopic = new DialogueTopic { TopicInfos = [nextTopicInfo] };
                            topicInfo.Links.Add(nextTopic);
                            processor.PreProcess(nextTopicInfo);
                        }

                        break;
                    default:
                        Console.WriteLine($"Warning: Didn't recognize {topicContent.GetType()} as branch Type");
                        break;
                }
            }
        }
    }

    private string GetText(ITextContainer paragraph) {
        var sb = new StringBuilder();
        foreach (IText text in paragraph.TextContent) {
            sb.Append(text.Text);
        }

        return sb.ToString();
    }

    private bool IsPlayerLine(ITextContainer paragraph) {
        foreach (IText text in paragraph.TextContent) {
            if (text is not FormatedText formattedText || formattedText.TextStyle.TextProperties.Bold is null)
                return false;
        }

        return true;
    }

    private List<FormattedText> GetFormattedText(ITextContainer paragraph) {
        return (from IText text in paragraph.TextContent select GetFormattedText(text))
            .NotNull()
            .ToList();
    }

    private FormattedText? GetFormattedText(IText text) {
        if (text.Text is null) return null;

        switch (text) {
            case FormatedText formattedText:
                if (string.IsNullOrWhiteSpace(formattedText.TextStyle.TextProperties.FontColor))
                    return new FormattedText(
                        text.Text,
                        formattedText.TextStyle.TextProperties.Bold is not null,
                        Color.Black);

                //Format "#112233"
                var r = Convert.ToInt32(formattedText.TextStyle.TextProperties.FontColor.Substring(1, 2), 16);
                var g = Convert.ToInt32(formattedText.TextStyle.TextProperties.FontColor.Substring(3, 2), 16);
                var b = Convert.ToInt32(formattedText.TextStyle.TextProperties.FontColor.Substring(5, 2), 16);

                return new FormattedText(
                    formattedText.Text,
                    formattedText.TextStyle.TextProperties.Bold is not null,
                    Color.FromArgb(r, g, b));
            default: return new FormattedText(text.Text, false, Color.Black);
        }
    }
}
