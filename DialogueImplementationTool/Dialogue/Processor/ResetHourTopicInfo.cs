using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class ResetHourTopicInfo(float hours) : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.ResetHours = hours;
    }
}
