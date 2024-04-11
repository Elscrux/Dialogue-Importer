namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class InvalidStringFixer : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var (invalid, fix) in InvalidString.InvalidStrings) {
            topicInfo.Prompt = topicInfo.Prompt.Replace(invalid, fix);
        }
    }
}
