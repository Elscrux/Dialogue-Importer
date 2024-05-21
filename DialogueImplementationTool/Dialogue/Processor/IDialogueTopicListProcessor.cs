using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public interface IDialogueTopicListProcessor {
    void Process(List<DialogueTopic> topics);
}
