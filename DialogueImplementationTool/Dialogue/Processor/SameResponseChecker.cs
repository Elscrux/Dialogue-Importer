using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

/// <summary>
/// Allows two player topics in a row to share the same line.
/// <example>
/// <para>Here we implicitly add "That's good!" to 1.1 as well.</para>
/// <code>
/// <para>1. Hi, Player. How are you?</para>
/// <para>	1.1. I'm good.</para>
/// <para>	1.2. I'm fine.</para>
/// <para>		1.2.1. That's good!</para>
/// </code>
/// </example>
/// </summary>
public sealed class SameResponseChecker : IConversationProcessor {
    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            CheckTopics(dialogue.Topics);
        }
    }

    private void CheckTopics(IList<DialogueTopic> topics) {
        var processedTopics = new HashSet<DialogueTopic>();
        var queueBacklog = new Queue<IList<DialogueTopic>>();
        queueBacklog.Enqueue(topics);

        // Iterate through all option lists to find ones with empty responses
        while (queueBacklog.Count > 0) {
            var currentTopicOptions = queueBacklog.Dequeue();
            for (var i = 0; i < currentTopicOptions.Count; i++) {
                var currentTopic = currentTopicOptions[i];
                if (!processedTopics.Add(currentTopic)) continue;

                foreach (var info in currentTopic.TopicInfos) {
                    queueBacklog.Enqueue(info.Links);
                }

                if (currentTopic.TopicInfos.TrueForAll(t => t.Responses.Count != 0)) continue;

                // We found a topic with no infos that have any responses
                // Search for the next topic with any responses and use its infos
                var nextTopic = currentTopicOptions
                    .Skip(i + 1)
                    .FirstOrDefault(t => t.TopicInfos.Exists(info => info.Responses.Count > 0));

                if (nextTopic is null) continue;

                currentTopic.TopicInfos.Clear();
                currentTopic.TopicInfos.AddRange(nextTopic.TopicInfos);
            }
        }
    }
}
