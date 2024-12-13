using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class GenericLineInvalidStringFixer : IGenericDialogueProcessor {
    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        foreach (var (invalid, fix) in InvalidString.InvalidStrings) {
            topicInfo.Responses[0].Response = topicInfo.Responses[0].Response.Replace(invalid, fix);
        }
    }
}
