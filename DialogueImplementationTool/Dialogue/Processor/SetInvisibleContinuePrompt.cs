using System.Linq;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class SetInvisibleContinuePrompt : IConversationProcessor {
    public void Process(Conversation conversation) {
        foreach (var topic in conversation.SelectMany(x => x.Topics).SelectMany(x => x.EnumerateLinks(true))) {
            foreach (var topicInfo in topic.TopicInfos) {
                if (!topicInfo.InvisibleContinue) continue;

                foreach (var link in topicInfo.Links) {
                    if (!link.GetPlayerText().IsNullOrEmpty()) continue;

                    link.TopicInfos[0].Prompt = "(invis cont)";
                }
            }
        }
    }
}
