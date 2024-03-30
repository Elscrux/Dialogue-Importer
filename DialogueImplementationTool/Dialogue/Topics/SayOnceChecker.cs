using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class SayOnceChecker : IDialogueTopicPostProcessor {
	private static readonly Regex InitialRegex = new(@"\[(initial)( (greeting))?\]", RegexOptions.IgnoreCase);

	public void Process(DialogueTopic topic) {
		if (topic.Responses.Count == 0) return;

		var response = topic.Responses[0];
		var previousResponse = response.Response;

		response.Response = InitialRegex.Replace(response.Response, string.Empty);

		if (response.Response != previousResponse) {
			topic.SayOnce = true;
		}
	}
}
