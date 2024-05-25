using System;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class BackToOptionsLinker : IConversationProcessor {
    public void Process(Conversation conversation) {
        foreach (var generatedDialogue in conversation) {
            // Just remove back to option links from base topic
            // Going back to options here is the default behavior
            foreach (var response in generatedDialogue.Topics.SelectMany(topic =>
                topic.TopicInfos.SelectMany(topicInfo => topicInfo.Responses))) {
                RemoveBackToOptions(response);
            }

            // Links can contain an explicit back to options link
            foreach (var topic in generatedDialogue.Topics.EnumerateLinks(true)) {
                foreach (var info in topic.TopicInfos) {
                    foreach (var link in info.Links) {
                        AddBackToOptionsLink(link, info);
                    }
                }
            }
        }
    }

    private static bool RemoveBackToOptions(DialogueResponse response) =>
        response.EndsNotes.RemoveAll(
            note => note.Text.Equals("back to options", StringComparison.Ordinal)) > 0;

    private static void AddBackToOptionsLink(DialogueTopic topic, DialogueTopicInfo incomingLink) {
        foreach (var topicInfo in topic.TopicInfos) {
            if (topicInfo.Responses.Count == 0) continue;

            // If back to options exists, add the links
            if (RemoveBackToOptions(topicInfo.Responses[^1])) topicInfo.Links.AddRange(incomingLink.Links);
        }
    }
}
