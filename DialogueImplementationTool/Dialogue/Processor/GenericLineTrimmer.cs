using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public class GenericLineTrimmer : IGenericDialogueProcessor {
    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        topicInfo.Responses[0].Response = topicInfo.Responses[0].Response.Trim();
    }
}
