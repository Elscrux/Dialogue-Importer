using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class ResetHourTopicInfo(float hours) : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.SayOnce) return;

        topicInfo.ResetHours = hours;
    }
}
