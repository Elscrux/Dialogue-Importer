using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using DialogueImplementationTool.Dialogue.Responses;
using DialogueImplementationTool.Dialogue.Topics;
using Noggog;
using Xceed.Words.NET;
using List = Xceed.Document.NET.List;
using Paragraph = Xceed.Document.NET.Paragraph;
namespace DialogueImplementationTool.Parser;

public sealed class DocXTextParser : DocumentParser {
    private const int FirstIndentationLevel = 0;
    
    private readonly DocX _doc;
    public override int LastIndex { get; }
    
    public DocXTextParser(string path) {
        var tryLoading = true;
        while (tryLoading) {
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        _doc ??= DocX.Create(Stream.Null);
        LastIndex = _doc.Lists.Count - 1;
    }

    public override void BacktrackMany() => Previous();

    public override void SkipMany() => Next();

    public override string Preview(int index) =>  index < 0 || index >= _doc.Lists.Count ? string.Empty : _doc.Lists[index].Items.FirstOrDefault()?.Text ?? string.Empty;

    protected override List<DialogueTopic> ParseDialogue(int index) {
        var branches = new List<DialogueTopic>();
        var list = _doc.Lists[index];
        
        if (!list.Items.Any()) return branches;

        //Evaluate if the player starts dialogue
        if (IsPlayerLine(list.Items[0])) {
            branches.AddRange(list.Items.Where(p => p.IndentLevel == FirstIndentationLevel)
                .Select(x => {
                    var currentBranch = AddTopic(x);
                    currentBranch.Build();
                    return currentBranch;
                }));
        } else {
            //One new branch, NPC starts to talk
            var currentBranch = new DialogueTopic();
            branches.Add(currentBranch);
        
            AddLinksAndResponses(list.Items[0], currentBranch);
            currentBranch.Build();
        }

        return branches;
    }

    protected override List<DialogueTopic> ParseOneLiner(int index) {
        var topics = new List<DialogueTopic>();
        
        var topic = new DialogueTopic();
        AddResponses(_doc.Lists[index], topic);
        topic.Build();
        topics.Add(topic);

        return topics;
    }

    protected override List<DialogueTopic> ParseScene(int index) => ParseOneLiner(index);

    private void AddResponses(List list, DialogueTopic topic) {
        if (!list.Items.Any()) return;

        foreach (var item in list.Items) {
            topic.Responses.Add( DialogueResponse.Build(GetFormattedText(item)));
        }
    }
    
    private DialogueTopic AddTopic(Paragraph paragraph) {
        var topic = new DialogueTopic();
        var startingIndentation = paragraph.IndentLevel;

        topic.Text = paragraph.Text;

        paragraph = paragraph.NextParagraph;
        if (paragraph.IndentLevel == startingIndentation + 1) {
            AddLinksAndResponses(paragraph, topic);
        }
    
        return topic;
    }
    
    private void AddLinksAndResponses(Paragraph paragraph, DialogueTopic topic) {
        var startingIndentation = paragraph.IndentLevel;
        
        //Add further responses
        for (; paragraph != null && paragraph.IndentLevel == startingIndentation; paragraph = paragraph.NextParagraph) {
            topic.Responses.Add(DialogueResponse.Build(GetFormattedText(paragraph)));
        }
        
        //Add links
        for (; paragraph != null && paragraph.IndentLevel > startingIndentation; paragraph = paragraph.NextParagraph) {
            if (paragraph.IndentLevel == startingIndentation + 1) {
                var nextTopic = AddTopic(paragraph);
                nextTopic.IncomingLink = topic;
                topic.Links.Add(nextTopic);
                nextTopic.Build();
            }
        }
    }
    
    private bool IsPlayerLine(Paragraph paragraph) => paragraph.MagicText.NotNull().All(magicText => magicText.formatting?.Bold is not (null or false));

    private IEnumerable<FormattedText> GetFormattedText(Paragraph paragraph) {
        return paragraph.MagicText
            .NotNull()
            .Select(text => new FormattedText(text.text, text.formatting?.Bold ?? false, text.formatting?.FontColor ?? Color.Black))
            .ToList();
    }
    
    private FormattedText GetFormattedText(Xceed.Document.NET.FormattedText text) {
        return new FormattedText(text.text, text.formatting.Bold ?? false, text.formatting.FontColor ?? Color.Black);
    }
}
