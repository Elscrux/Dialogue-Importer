using System;
namespace DialogueImplementationTool.Dialogue.Topics; 

public class GoodbyeChecker : IDialogueTopicPostProcessor {
	public void Process(DialogueTopic topic) {
		if (topic.Responses.Count == 0) return;

		var response = topic.Responses[0];
		var previousResponse = response.Response;

		response.Response = response.Response
			.Replace("[exit conversation]", string.Empty, StringComparison.OrdinalIgnoreCase);

		if (response.Response != previousResponse) {
			topic.SayOnce = true;
		}
	}
}
