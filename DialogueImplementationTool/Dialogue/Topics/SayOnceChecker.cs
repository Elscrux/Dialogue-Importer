using System;
namespace DialogueImplementationTool.Dialogue.Topics; 

public class SayOnceChecker : IDialogueTopicPostProcessor {
    public void Process(DialogueTopic topic) {
        if (topic.Responses.Count == 0) return;

        var response = topic.Responses[0];
        var previousResponse = response.Response;
            
        response.Response = response.Response
            .Replace("[initial]", string.Empty, StringComparison.InvariantCulture);

        if (response.Response != previousResponse) {
            topic.SayOnce = true;
        }
    }
}
