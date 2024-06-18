using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class TrainServiceChecker : IConversationProcessor {
    [GeneratedRegex("I'd like to train|Can you teach me")]
    public static partial Regex TrainRegex();

    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics) {
                Process(topic);
            }
        }
    }

    public void Process(DialogueTopic topic) {
        if (topic.TopicInfos.Count == 0) return;
        if (!TrainRegex().IsMatch(topic.GetPlayerText())) return;

        topic.ConvertResponsesToTopicInfos();
        topic.ServiceType = ServiceType.Train;
        foreach (var topicInfo in topic.TopicInfos) {
            topicInfo.Script.EndScriptLines.Add("Game.ShowTrainingMenu(akSpeaker)");
            foreach (var response in topicInfo.Responses) {
                response.RemoveNote(note => note.Contains("train"));
            }
        }
    }
}
