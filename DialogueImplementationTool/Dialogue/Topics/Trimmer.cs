using Noggog;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class Trimmer : IDialogueTopicPostProcessor {
	public void Process(DialogueTopic topic) {
		topic.Text = topic.Text.Trim();
		topic.Responses.ForEach(r => r.Response = r.Response.Trim());
		topic.Responses.RemoveWhere(r => r.Response.IsNullOrEmpty());
	}
}
