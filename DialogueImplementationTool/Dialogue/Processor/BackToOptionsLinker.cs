using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class BackToOptionsLinker : IConversationProcessor {
    public void Process(IList<GeneratedDialogue> dialogues) {
        foreach (var generatedDialogue in dialogues) {
            // Just remove back to option links from base  
            foreach (var response in generatedDialogue.Topics.SelectMany(topic => topic.TopicInfos.SelectMany(topicInfo => topicInfo.Responses))) {
                response.Response = RemoveBackToOptions(response);
            }

            // Links can contain an explicit back to options links
            foreach (var topic in generatedDialogue.Topics.EnumerateLinks()) {
                foreach (var info in topic.TopicInfos) {
                    foreach (var link in info.Links) {
                        AddBackToOptionsLink(link, info);
                    }
                }
            }
        }
    }

    private static string RemoveBackToOptions(DialogueResponse response) => response.Response
            .Replace("[back to options]", string.Empty, StringComparison.InvariantCulture)
            .Trim();

    private static void AddBackToOptionsLink(DialogueTopic topic, DialogueTopicInfo incomingLink) {
        foreach (var topicInfo in topic.TopicInfos) {
            if (topicInfo.Responses.Count == 0) continue;

            // Check for back to options link
            var response = topicInfo.Responses[^1];
            var previousResponse = response.Response;

            response.Response = RemoveBackToOptions(response);

            // If back to options exists, add the links
            if (response.Response != previousResponse) topicInfo.Links.AddRange(incomingLink.Links);
        }
    }
}
