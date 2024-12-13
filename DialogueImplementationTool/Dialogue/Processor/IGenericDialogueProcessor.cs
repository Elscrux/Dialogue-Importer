using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public interface IGenericDialogueProcessor {
    void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo);
}
