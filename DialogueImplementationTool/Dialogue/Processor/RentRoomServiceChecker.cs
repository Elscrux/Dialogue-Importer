using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class RentRoomServiceChecker : IConversationProcessor {
    [GeneratedRegex(@"I'd like to rent a room\.")]
    public static partial Regex RentRoomRegex { get; }

    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics) {
                Process(topic);
            }
        }
    }

    public void Process(DialogueTopic topic) {
        if (topic.TopicInfos.Count == 0) return;
        if (!RentRoomRegex.IsMatch(topic.GetPlayerText())) return;

        topic.ConvertResponsesToTopicInfos();
        topic.ServiceType = ServiceType.RentRoom;
        foreach (var topicInfo in topic.TopicInfos) {
            topicInfo.Script.EndScriptLines.Add(
                "(akSpeaker as RentRoomScript).RentRoom(GetOwningQuest() as DialogueGenericScript)");
            topicInfo.Random = true;
        }
    }
}
