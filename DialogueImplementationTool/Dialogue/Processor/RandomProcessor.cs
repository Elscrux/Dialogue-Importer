using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class RandomProcessor : IGenericDialogueProcessor {

    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        topicInfo.Random = true;
    }
}
