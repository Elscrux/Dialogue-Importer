﻿using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class VendorServiceChecker : IConversationProcessor {
    [GeneratedRegex(@"What do you have for sale\?|What have you got for sale\?|I'd like some food and drink\.")]
    public static partial Regex VendorRegex();

    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics) {
                Process(topic);
            }
        }
    }

    public void Process(DialogueTopic topic) {
        if (topic.TopicInfos.Count == 0) return;
        if (!VendorRegex().IsMatch(topic.GetPlayerText())) return;

        topic.ConvertResponsesToTopicInfos();
        topic.ServiceType = ServiceType.Vendor;
        foreach (var topicInfo in topic.TopicInfos) {
            topicInfo.Script.EndScriptLines.Add("akSpeaker.ShowBarterMenu()");
            topicInfo.ExtraConditions.Add(new ConditionFloat {
                Data = new GetOffersServicesNowConditionData(),
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1,
            });
        }
    }
}