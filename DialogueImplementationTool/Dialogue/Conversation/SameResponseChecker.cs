using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Conversation;

public sealed class SameResponseChecker : IConversationProcessor {
    public void Process(IList<GeneratedDialogue> dialogues) {
        foreach (var dialogue in dialogues) {
            CheckTopics(dialogue.Topics);
        }
    }

    private void CheckTopics(IList<DialogueTopic> topics) {
        var processedTopics = new HashSet<DialogueTopic>();
        var queueBacklog = new Queue<IList<DialogueTopic>>();
        queueBacklog.Enqueue(topics);

        while (queueBacklog.Count > 0) {
            var dialogueTopics = queueBacklog.Dequeue();
            for (var i = 0; i < dialogueTopics.Count; i++) {
                var currentTopic = dialogueTopics[i];
                if (processedTopics.Contains(currentTopic)) continue;

                processedTopics.Add(currentTopic);
                queueBacklog.Enqueue(currentTopic.Links);
                if (currentTopic.Responses.Count != 0) continue;

                // We found an empty topic
                // Search for the next topic with any responses and use those
                for (var j = i + 1; j < dialogueTopics.Count; j++) {
                    if (dialogueTopics[j].Responses.Count == 0) continue;

                    currentTopic.Responses.AddRange(dialogueTopics[j].Responses);
                    break;
                }
            }
        }
    }
}