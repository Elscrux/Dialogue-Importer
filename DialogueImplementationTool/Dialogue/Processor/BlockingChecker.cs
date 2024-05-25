using System.Linq;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class BlockingChecker : IConversationProcessor {
    public void Process(Conversation conversation) {
        foreach (var topic in conversation.SelectMany(x => x.Topics)) {
            if (topic.TopicInfos is not [var topicInfo]) continue;

            if (topicInfo.Prompt.IsNullOrWhitespace()) {
                topic.TopicInfos[0].Prompt = "(blocking)";
                topic.Blocking = true;
            }
        }
    }
}
