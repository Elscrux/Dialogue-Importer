using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class SceneProcessor(IDialogueContext context) : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Scene;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        var voiceType = GenericMetaData.GetVoiceType(topicInfo.MetaData);

        switch (description) {
            case "Player enters NPC home":
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, new WIHouseQuestFactory(context, voiceType));
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context, 1));
                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
            case "Player enters NPC home with weapon drawn":
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, new WIHouseQuestFactory(context, voiceType));
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context, 1));
                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();
                yield return new IsWeaponOutConditionData {
                    RunOnType = Condition.RunOnType.Reference,
                    Reference = Skyrim.PlayerRef,
                }.ToConditionFloat(comparisonValue: 2);

                break;
            case "Player drops an item (comment about littering)":
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, new WIRemoveItemTrashQuestFactory(context, voiceType));
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context, 0));
                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
        }
    }
}
