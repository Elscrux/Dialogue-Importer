using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public class RandomProcessor : IGenericDialogueProcessor {

    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        topicInfo.Random = true;
    }
}
