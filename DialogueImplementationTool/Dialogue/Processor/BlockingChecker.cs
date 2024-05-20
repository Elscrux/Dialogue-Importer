using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class BlockingChecker : IConversationProcessor {
    public void Process(Conversation conversation) {
        foreach (var topic in conversation.SelectMany(x => x.Topics)) {
            if (topic.TopicInfos is not [var topicInfo]) continue;

            if (topicInfo.Prompt.IsNullOrWhitespace()) topic.Blocking = true;
        }
    }
}
