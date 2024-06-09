using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class RumorServiceChecker : IConversationProcessor {
    [GeneratedRegex(@"Heard any (good )?rumors lately\?")]
    public static partial Regex RumorRegex();

    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics) {
                Process(topic);
            }
        }
    }

    public void Process(DialogueTopic topic) {
        if (topic.TopicInfos.Count == 0) return;
        if (!RumorRegex().IsMatch(topic.GetPlayerText())) return;

        topic.ConvertResponsesToTopicInfos();
        topic.ServiceType = ServiceType.Rumor;
        foreach (var topicInfo in topic.TopicInfos) {
            topicInfo.Random = true;
        }
    }
}
