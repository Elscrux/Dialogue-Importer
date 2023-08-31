using System.Collections.Generic;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Conversation;

public sealed class BlockingChecker : IConversationProcessor {
	public void Process(IList<GeneratedDialogue> dialogues) {
		foreach (var dialogue in dialogues) {
			foreach (var topic in dialogue.Topics) {
				if (topic.Text.IsNullOrWhitespace()) {
					topic.Blocking = true;
				}
			}
		}
	}
}
