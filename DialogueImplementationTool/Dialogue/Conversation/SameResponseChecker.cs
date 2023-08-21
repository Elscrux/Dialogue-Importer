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
        for (var i = 0; i < topics.Count; i++) {
            var currentTopic = topics[i];
            CheckTopics(currentTopic.Links);
            if (currentTopic.Responses.Count != 0) continue;

            // We found an empty topic
            // Search for the next topic with any responses and use those
            for (var j = i + 1; j < topics.Count; j++) {
                if (topics[j].Responses.Count == 0) continue;

                currentTopic.Responses.AddRange(topics[j].Responses);
                break;
            }
        }
    }
}