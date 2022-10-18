using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using AODL.Document.Content;
using AODL.Document.Content.Text;
using AODL.Document.Content.Text.TextControl;
using AODL.Document.TextDocuments;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Responses;
using DialogueImplementationTool.Dialogue.Topics;
namespace DialogueImplementationTool.Parser;

public sealed class OpenDocumentTextParser : DocumentParser {
    private readonly TextDocument _doc = new();

    public override int LastIndex { get; }

    public override void BacktrackMany() {
        if (Index == 0) return;
        
        for (var i = Index - 1; i >= 0; i--) {
            if (_doc.Content[i] is not List) continue;

            Index = i;
            return;
        }
        
        Index = 0;
    }
    
    public override void SkipMany() {
        if (Index >= _doc.Content.Count - 1) return;
        
        for (var i = Index + 1; i < _doc.Content.Count; i++) {
            if (_doc.Content[i] is not List) continue;

            Index = i;
            return;
        }
        
        Index = LastIndex;
    }

    public override string Preview(int index) {
        switch  (_doc.Content[index])  {
            case List list:
                while (list.Content.Count > 0) {
                    if (list.Content[0] is ListItem listItem) {
                        if (listItem.Content.Count > 0) {
                            switch (listItem.Content[0]) {
                                case Paragraph paragraph:
                                    return GetText(paragraph);
                                case List newList:
                                    list = newList;
                                    break;
                                default:
                                    return string.Empty;
                            }
                        } else return string.Empty;
                    } else return string.Empty;
                }
                
                break;
            case Paragraph paragraph:
                return GetText(paragraph);
        }

        return string.Empty;
    } 

    public OpenDocumentTextParser(string path) {
        var tryLoading = true;
        while (tryLoading) {
            try {
                _doc.Load(path);
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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

    private void MergeLists() {
        var startingIndex = 0;
        while (startingIndex < _doc.Content.Count) {
            if (_doc.Content[startingIndex] is not List currentList) {
                startingIndex++;
                continue;
            }

            var listAdded = false;
            var addNextList = false;
            var index = startingIndex + 1;
            while (index < _doc.Content.Count) {
                switch (_doc.Content[index]) {
                    case Paragraph paragraph:
                        if (paragraph.TextContent.Count == 0) {
                            addNextList = true;
                            _doc.Content.RemoveAt(index);
                        } else {
                            goto EndPoint;
                        }
                        break;
                    case List list:
                        if (addNextList) {
                            foreach (IContent content in list.Content) {
                                currentList.Content.Add(content);
                            }
                            _doc.Content.RemoveAt(index);
                            listAdded = true;
                            addNextList = false;
                        } else {
                            goto EndPoint;
                        }
                        break;
                    default:
                        goto EndPoint;
                }
            }
            
            EndPoint: ;

            if (!listAdded) {
                startingIndex++;
            }
        }
    }

    protected override List<DialogueTopic> ParseDialogue(int index) {
        var branches = new List<DialogueTopic>();
        if (_doc.Content[index] is not List list) return branches;

        //Evaluate if the player starts dialogue
        var playerDialogue = true;
        if (list.Content.Count > 0) {
            if (list.Content[0] is ListItem listItem) {
                if (listItem.Content.Count > 0) {
                    if (listItem.Content[0] is Paragraph paragraph) {
                        playerDialogue = IsPlayerLine(paragraph);
                    } else Console.WriteLine($"Warning: Didn't recognize {listItem.Content[0].GetType()} as paragraph type");
                }
            } else Console.WriteLine($"Warning: Didn't recognize {list.Content[0].GetType()} as list item type");
        }

        if (playerDialogue) {
            foreach (IContent branch in list.Content) {
                if (branch is not ListItem branchItem) continue;
                    
                //Player dialogue - every entry is a new branch
                branches.Add(AddTopic(branchItem));
            }
        } else {
            //One new branch, NPC starts to talk
            var currentBranch = new DialogueTopic();
            branches.Add(currentBranch);

            AddLinksAndResponses(list, currentBranch);
        }

        return branches;
    }

    protected override List<DialogueTopic> ParseOneLiner(int index) {
        var topics = new List<DialogueTopic>();
        if (_doc.Content[index] is not List list) return topics;
        
        var topic = new DialogueTopic();
        AddResponses(list, topic);
        topics.Add(topic);

        return topics;
    }

    protected override List<DialogueTopic> ParseScene(int index) => ParseOneLiner(index);

    private void AddResponses(IContentContainer list, DialogueTopic topic) {
        foreach (IContent listContent in list.Content) {
            if (listContent is not ListItem listItem) continue;
            
            foreach (IContent itemContent in listItem.Content) {
                switch (itemContent) {
                    case Paragraph paragraph:
                        //Set player text
                        topic.Responses.Add(DialogueResponse.Build(GetFormattedText(paragraph)));

                        break;
                    default:
                        Console.WriteLine($"Warning: Didn't recognize {listContent.GetType()} as response type");

                        break;
                }
            }
        }
    }
    
    private DialogueTopic AddTopic(IContentContainer listItem) {
        var topic = new DialogueTopic();
        
        foreach (IContent itemContent in listItem.Content) {
            switch (itemContent) {
                case Paragraph paragraph:
                    //Set player text
                    topic.Text = GetText(paragraph);

                    break;
                case List linksAndResponsesList:
                    //Add links and responses
                    AddLinksAndResponses(linksAndResponsesList, topic);

                    break;
                default:
                    Console.WriteLine($"Warning: Didn't recognize {itemContent.GetType()} as topic type");

                    break;
            }
        }

        return topic;
    }
    
    private void AddLinksAndResponses(IContentContainer list, DialogueTopic topic) {
        foreach (IContent listContent in list.Content) {
            if (listContent is not ListItem listItem) continue;
                    
            foreach (IContent topicContent in listItem.Content) {
                switch (topicContent) {
                    case Paragraph paragraph:
                        //Add responses
                        topic.Responses.Add(DialogueResponse.Build(GetFormattedText(paragraph)));
                        break;
                    case List linkList:
                        //Add links
                        foreach (IContent linkContent in linkList.Content) {
                            if (linkContent is not ListItem linkItem) continue;
                            
                            topic.Links.Add(AddTopic(linkItem));
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
            if (text is not FormatedText formattedText || formattedText.TextStyle.TextProperties.Bold == null) {
                return false;
            }
        }

        return true;
    }
    
    private List<FormattedText> GetFormattedText(ITextContainer paragraph) {
        return (from IText text in paragraph.TextContent select GetFormattedText(text)).ToList();
    }
    
    private FormattedText GetFormattedText(IText text) {
        switch (text) {
            case FormatedText formattedText:
                if (string.IsNullOrWhiteSpace(formattedText.TextStyle.TextProperties.FontColor)) {
                    return new FormattedText(text.Text, formattedText.TextStyle.TextProperties.Bold != null, Color.Black);
                }
                
                //Format "#112233"
                var r = Convert.ToInt32(formattedText.TextStyle.TextProperties.FontColor.Substring(1, 2), 16);
                var g = Convert.ToInt32(formattedText.TextStyle.TextProperties.FontColor.Substring(3, 2), 16);
                var b = Convert.ToInt32(formattedText.TextStyle.TextProperties.FontColor.Substring(5, 2), 16);

                return new FormattedText(formattedText.Text, formattedText.TextStyle.TextProperties.Bold != null, Color.FromArgb(r, g, b));
            case WhiteSpace:
            case SimpleText:
                return new FormattedText(text.Text, false, Color.Black);
            default:
                throw new ArgumentOutOfRangeException(nameof(text));

        }
    }
}
