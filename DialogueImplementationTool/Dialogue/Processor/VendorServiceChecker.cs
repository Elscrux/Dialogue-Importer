using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class VendorServiceChecker : IConversationProcessor {
    [GeneratedRegex(@"What do you have for sale\?|What have you got for sale\?|I'd like some food and drink\.|Show me what you have for sale\.|Show me what you've got for sale\.")]
    public static partial Regex VendorRegex { get; }

    [GeneratedRegex("vendor|shop")]
    public static partial Regex VendorNoteRegex { get; }

    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics) {
                Process(topic);
            }
        }
    }

    public void Process(DialogueTopic topic) {
        if (topic.TopicInfos.Count == 0) return;
        if (!VendorRegex.IsMatch(topic.GetPlayerText())) return;

        topic.ConvertResponsesToTopicInfos();
        topic.ServiceType = ServiceType.Vendor;
        foreach (var topicInfo in topic.TopicInfos) {
            foreach (var response in topicInfo.Responses) {
                response.RemoveNote(note => VendorNoteRegex.IsMatch(note));
            }

            topicInfo.Prompt.Text = "";
            topicInfo.Script.EndScriptLines.Add("akSpeaker.ShowBarterMenu()");
            topicInfo.ExtraConditions.Add(new ConditionFloat {
                Data = new GetOffersServicesNowConditionData(),
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1,
            });
        }
    }
}
