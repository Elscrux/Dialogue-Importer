using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class BackToOptionsLinker : IConversationProcessor {
    [GeneratedRegex("(?:return|back|go) to( dialog(ue)?)? options")]
    private static partial Regex Regex { get; }

    public void Process(Conversation conversation) {
        var seenTopics = new HashSet<DialogueTopic>();
        
        foreach (var generatedDialogue in conversation) {
            // Just remove back to option links from base topic
            // Going back to options here is the default behavior
            foreach (var response in generatedDialogue.Topics.SelectMany(topic =>
                topic.TopicInfos.SelectMany(topicInfo => topicInfo.Responses))) {
                RemoveBackToOptions(response);
            }

            // Links can contain an explicit back to options link
            foreach (var topic in generatedDialogue.Topics) {
                Process(topic, null);
            }
        }

        void Process(DialogueTopic topic, DialogueTopicInfo? incomingLinkNotInvisibleContinue) {
            if (!seenTopics.Add(topic)) return;

            foreach (var info in topic.TopicInfos) {
                foreach (var link in info.Links) {
                    var linkNotInvisibleContinue = info.InvisibleContinue ? incomingLinkNotInvisibleContinue : info;
                    Process(link, linkNotInvisibleContinue);
                    if (linkNotInvisibleContinue is not null) {
                        AddBackToOptionsLink(link, linkNotInvisibleContinue);
                    }
                }
            }
        }
    }

    private static bool RemoveBackToOptions(DialogueResponse response) =>
        response.EndsNotes.RemoveAll(note => Regex.IsMatch(note.Text)) > 0;

    private static void AddBackToOptionsLink(DialogueTopic topic, DialogueTopicInfo incomingLink) {
        foreach (var topicInfo in topic.TopicInfos) {
            if (topicInfo.Responses.Count == 0) continue;

            // If back to options exists, add the links
            if (RemoveBackToOptions(topicInfo.Responses[^1])) topicInfo.Links.AddRange(incomingLink.Links);
        }
    }
}
