using System.Collections.Generic;
using DialogueImplementationTool.Dialogue;
namespace DialogueImplementationTool.Parser;

public class NullDocumentParser : DocumentParser {
    public override int LastIndex => int.MaxValue;
    public override void SkipMany() {
        throw new System.NotImplementedException();
    }
    public override void BacktrackMany() {
        throw new System.NotImplementedException();
    }
    public override string Preview(int index) {
        throw new System.NotImplementedException();
    }
    protected override List<DialogueTopic> ParseDialogue(int index) {
        throw new System.NotImplementedException();
    }
    protected override List<DialogueTopic> ParseOneLiner(int index) {
        throw new System.NotImplementedException();
    }
    protected override List<DialogueTopic> ParseScene(int index) {
        throw new System.NotImplementedException();
    }
}
