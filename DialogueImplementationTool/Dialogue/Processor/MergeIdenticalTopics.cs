using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class MergeIdenticalTopics : IConversationProcessor {
    public void Process(Conversation conversation) {
        // Group by topic info count to reduce the number of comparisons
        foreach (var topicGrouping in conversation
            .SelectMany(x => x.Topics)
            .SelectMany(x => x.EnumerateLinks(true))
            .GroupBy(x => x.TopicInfos.Count)) {
            var topics = topicGrouping.ToList();

            // Check if any topic is identical
            while (topics.Count > 0) {
                var equivalentTopics = topics
                    .Where(x => x.Equals(topics[0]))
                    .ToList();

                MergeTopics(equivalentTopics);

                foreach (var topic in equivalentTopics) {
                    topics.Remove(topic);
                }
            }
        }

        void MergeTopics(IReadOnlyList<DialogueTopic> equivalentTopics) {
            if (equivalentTopics.Count <= 1) return;
            if (equivalentTopics.All(x => ReferenceEquals(x, equivalentTopics[0]))) return;

            // Enumerate all links and set them to the first topics
            foreach (var topic in conversation.SelectMany(x => x.Topics).SelectMany(x => x.EnumerateLinks(true))) {
                foreach (var topicInfo in topic.TopicInfos) {
                    for (var i = 0; i < topicInfo.Links.Count; i++) {
                        var link = topicInfo.Links[i];

                        // Set all equivalent topics to the first topic
                        foreach (var equivalentTopic in equivalentTopics.Skip(1)) {
                            if (Equals(link, equivalentTopic)) {
                                topicInfo.Links[i] = equivalentTopics[0];
                            }
                        }
                    }
                }
            }
        }
    }
}
