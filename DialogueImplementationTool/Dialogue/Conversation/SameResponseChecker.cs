﻿using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Conversation;

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
				if (!processedTopics.Add(currentTopic)) continue;

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
