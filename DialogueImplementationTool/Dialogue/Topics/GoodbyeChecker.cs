using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Topics;

public class GoodbyeChecker : IDialogueTopicPostProcessor {
	private static readonly Regex GoodbyeRegex = new(@"\[(exit|end) (dialog|dialogue|conversation|convo)\]");

	public void Process(DialogueTopic topic) {
		if (topic.Responses.Count == 0) return;

		var response = topic.Responses[0];
		var previousResponse = response.Response;

		response.Response = GoodbyeRegex.Replace(response.Response, string.Empty);

		if (response.Response != previousResponse) {
			topic.Goodbye = true;
		}
	}
}
