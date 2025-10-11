using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class CustomProcessor(IDialogueContext context) : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Custom;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        var voiceType = GenericMetaData.GetVoiceType(topicInfo.MetaData);

        switch (description) {
            case "NPC returns the dropped item to the player": {
                var questFactory = new WIRemoveItemReturnQuestFactory(context, voiceType);
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, questFactory);
                GenericMetaData.SetPostProcessor(topicInfo.MetaData, new WIRemoveItemReturnPostProcessor(context));

                // Add script to return the item to the player
                topicInfo.Script.StartScriptLines.Add(questFactory.GetReturnItemScript());

                // Add conditions
                yield return questFactory.GetIsBystanderAliasCondition();
                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
            }
            case "Player casts Calm on NPC": {
                var questFactory = new CustomWICastMagicReactionFactory(
                    context,
                    "Calm",
                    "NPC reacts to calm being cast on them",
                    FormKey.Factory("05EBDC:BSAssets.esm"),
                    []);

                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, questFactory);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context, 0));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();
                break;
            }
            case "Player casts Courage on NPC": {
                var questFactory = new CustomWICastMagicReactionFactory(
                    context,
                    "Courage",
                    "NPC reacts to courage being cast on them",
                    FormKey.Factory("05EBE0:BSAssets.esm"),
                    []);

                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, questFactory);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context, 0));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();
                break;
            }
            case "Player casts Healing spell on NPC": {
                var questFactory = new CustomWICastMagicReactionFactory(
                    context,
                    "Healing",
                    "NPC reacts to healing being cast on them",
                    FormKey.Factory("05EBE1:BSAssets.esm"),
                    []);

                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, questFactory);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context, 0));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();
                break;
            }
            case "Player casts Stealth spell on NPC": {
                var questFactory = new CustomWICastMagicReactionFactory(
                    context,
                    "Stealth",
                    "NPC reacts to stealth being cast on them",
                    FormKey.Factory("05EBE2:BSAssets.esm"),
                    []);

                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, questFactory);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context, 0));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();
                break;
            }
            case "Player casts other non-hostile spell on NPC": {
                var questFactory = new CustomWICastMagicReactionFactory(
                    context,
                    "Weird",
                    "NPC reacts to weird spell being cast on them",
                    FormKey.Factory("05EBE3:BSAssets.esm"),
                    []);

                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, questFactory);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context, 0));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();
                break;
            }
        }
    }
}
