using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
namespace DialogueImplementationTool.Parser;

public class NullDocumentParser : DocumentParser {
    public override int LastIndex => int.MaxValue;
    public override void SkipMany() {
        throw new NotImplementedException();
    }
    public override void BacktrackMany() {
        throw new NotImplementedException();
    }
    public override string Preview(int index) {
        throw new NotImplementedException();
    }
    protected override List<DialogueTopic> ParseDialogue(int index) {
        throw new NotImplementedException();
    }
    protected override List<DialogueTopic> ParseOneLiner(int index) {
        throw new NotImplementedException();
    }
    protected override List<DialogueTopic> ParseScene(int index) {
        throw new NotImplementedException();
    }
}
