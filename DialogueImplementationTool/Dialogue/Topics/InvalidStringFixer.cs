namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class InvalidStringFixer : IDialogueTopicPostProcessor {
	public void Process(DialogueTopic topic) {
		foreach (var (invalid, fix) in InvalidString.InvalidStrings) {
			topic.Text = topic.Text.Replace(invalid, fix);
		}
	}
}
