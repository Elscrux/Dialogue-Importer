using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue;
using Xceed.Document.NET;
using Xceed.Words.NET;
namespace DialogueImplementationTool.Parser;

public sealed class DocXTextParser : DocumentParser {
    private const int FirstIndentationLevel = 0;
    
    private readonly DocX _doc;
    public override int LastIndex { get; }
    
    public DocXTextParser(string path) {
        _doc = DocX.Load(path);
        
        LastIndex = _doc.Lists.Count - 1;
    }

    public override void BacktrackMany() => Previous();

    public override void SkipMany() => Next();

    public override string Preview(int index) => _doc.Lists[index].Items.FirstOrDefault()?.Text ?? string.Empty;

    protected override List<DialogueTopic> ParseDialogue(int index) {
        var branches = new List<DialogueTopic>();
        var list = _doc.Lists[index];
        
        if (!list.Items.Any()) return branches;

        //Evaluate if the player starts dialogue
        if (IsPlayerLine(list.Items[0])) {
            branches.AddRange(list.Items.Where(p => p.IndentLevel == FirstIndentationLevel).Select(AddTopic));
        } else {
            //One new branch, NPC starts to talk
            var currentBranch = new DialogueTopic();
            branches.Add(currentBranch);
        
            AddLinksAndResponses(list.Items[0], currentBranch);
        }

        return branches;
    }

    protected override List<DialogueTopic> ParseOneLiner(int index) {
        var topics = new List<DialogueTopic>();
        
        var topic = new DialogueTopic();
        AddResponses(_doc.Lists[index], topic);
        topics.Add(topic);

        return topics;
    }

    protected override List<DialogueTopic> ParseScene(int index) => ParseOneLiner(index);

    private static void AddResponses(List list, DialogueTopic topic) {
        if (!list.Items.Any()) return;

        foreach (var item in list.Items) topic.Responses.Add(item.Text);
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
        for (; paragraph.IndentLevel == startingIndentation; paragraph = paragraph.NextParagraph) {
            topic.Responses.Add(paragraph.Text);
        }
        
        //Add links
        for (; paragraph.IndentLevel > startingIndentation; paragraph = paragraph.NextParagraph) {
            if (paragraph.IndentLevel == startingIndentation + 1) {
                topic.Links.Add(AddTopic(paragraph));
            }
        }
    }
    
    private static bool IsPlayerLine(Paragraph paragraph) => paragraph.MagicText.All(magicText => magicText.formatting.Bold is not (null or false));
}
