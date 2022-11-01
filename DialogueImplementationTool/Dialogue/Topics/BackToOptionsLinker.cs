using System;
namespace DialogueImplementationTool.Dialogue.Topics; 

public class BackToOptionsLinker : IDialogueTopicPostProcessor {
    public void Process(DialogueTopic topic) {
        if (topic.Responses.Count == 0) return;

        var response = topic.Responses[^1];
        var previousResponse = response.Response;
            
        response.Response = response.Response
            .Replace("[back to options]", string.Empty, StringComparison.InvariantCulture);

        if (response.Response != previousResponse && topic.IncomingLink != null) {
            topic.Links.AddRange(topic.IncomingLink.Links);
        }
    }
}
