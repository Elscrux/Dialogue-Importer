using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Conversation;

public sealed class BackToOptionsLinker : IConversationProcessor {
    public void Process(IList<GeneratedDialogue> dialogues) {
        foreach (var generatedDialogue in dialogues) {
            foreach (var topic in generatedDialogue.Topics.EnumerateLinks()) {
                foreach (var info in topic.TopicInfos) {
                    foreach (var link in info.Links) {
                        AddBackToOptionsLink(link, info);
                    }
                }
            }
        }
    }

    private static void AddBackToOptionsLink(DialogueTopic topic, DialogueTopicInfo incomingLink) {
        foreach (var topicInfo in topic.TopicInfos) {
            if (topicInfo.Responses.Count == 0) continue;

            // Check for back to options link
            var response = topicInfo.Responses[^1];
            var previousResponse = response.Response;

            response.Response = response.Response
                .Replace("[back to options]", string.Empty, StringComparison.InvariantCulture);

            // If back to options exists, add the links
            if (response.Response != previousResponse) topicInfo.Links.AddRange(incomingLink.Links);
        }
    }
}
