using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TopicInfoInvalidStringFixer : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var (invalid, fix) in InvalidString.InvalidStrings) {
            topicInfo.Prompt = topicInfo.Prompt.Replace(invalid, fix);
        }
    }
}
