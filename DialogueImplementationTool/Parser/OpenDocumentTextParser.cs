using System;
using System.Collections.Generic;
using System.Text;
using AODL.Document.Content;
using AODL.Document.Content.Text;
using AODL.Document.TextDocuments;
using DialogueImplementationTool.Dialogue;
namespace DialogueImplementationTool.Parser;

public sealed class OpenDocumentTextParser : DocumentParser {
    private readonly TextDocument _doc = new();
    private int _index;

    public OpenDocumentTextParser(string path) {
        _doc.Load(path);
    }

    public override List<DialogueTopic> ParseNext() {
        var branches = new List<DialogueTopic>();
        if (HasFinished()) return branches;

        if (_doc.Content[_index] is not List list) return branches;
        
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

        _index++;

        return branches;
    }
    
    public override string PreviewCurrent() {
        if (HasFinished()) return string.Empty;
        
        switch  (_doc.Content[_index])  {
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

    public override bool HasFinished() {
        return _doc.Content.Count <= _index;
    }
    
    public override void SkipOne() {
        _index++;
    }
    
    public override void SkipMany() {
        for (var i = _index; i < _doc.Content.Count; i++) {
            if (_doc.Content[i] is not List) continue;

            _index = i;
            return;
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
                        if (topic.Responses.Count == 0) {
                            topic.Responses.Add(new List<string>());                            
                        }

                        topic.Responses[0].Add(GetText(paragraph));
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
}
