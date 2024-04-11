using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
namespace DialogueImplementationTool.Parser;

public sealed class NullDocumentParser(string filePath) : IDocumentParser {
    public string FilePath { get; }
    public DialogueProcessor DialogueProcessor { get; }
    public int Index { get; set; }
    public int LastIndex { get; }

    public void SkipMany() {
        throw new NotImplementedException();
    }

    public void BacktrackMany() {
        throw new NotImplementedException();
    }

    public string Preview(int index) {
        throw new NotImplementedException();
    }


    public List<DialogueTopic> ParseDialogue(int index) {
        throw new NotImplementedException();
    }

    public List<DialogueTopic> ParseOneLiner(int index) {
        throw new NotImplementedException();
    }

    public List<DialogueTopic> ParseScene(int index) {
        throw new NotImplementedException();
    }
}
