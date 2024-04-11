using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Conversation;

public sealed class BlockingChecker : IConversationProcessor {
    public void Process(IList<GeneratedDialogue> dialogues) {
        foreach (var topic in dialogues.SelectMany(x => x.Topics)) {
            if (topic.TopicInfos is not [var topicInfo]) continue;

            if (topicInfo.Prompt.IsNullOrWhitespace()) topic.Blocking = true;
        }
    }
}
