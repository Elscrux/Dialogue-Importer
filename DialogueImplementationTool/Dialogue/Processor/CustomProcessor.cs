using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class CustomProcessor(IDialogueContext context) : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Custom;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        var voiceType = GenericMetaData.GetVoiceType(topicInfo.MetaData);

        switch (description) {
            case "NPC returns the dropped item to the player":
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
            case "Player casts Calm on NPC": {
                var dialogTopic =
                    context.GetOrAddOverride<DialogTopic, IDialogTopicGetter>(Skyrim.DialogTopic
                        .WICastMagicNonHostileSpellCalmTopic);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new ExistingDialogueTopicFactory(dialogTopic));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
            }
            case "Player casts Courage on NPC": {
                var dialogTopic =
                    context.GetOrAddOverride<DialogTopic, IDialogTopicGetter>(Skyrim.DialogTopic
                        .WICastMagicNonHostileSpellCourageTopic);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new ExistingDialogueTopicFactory(dialogTopic));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
            }
            case "Player casts Healing spell on NPC": {
                var dialogTopic =
                    context.GetOrAddOverride<DialogTopic, IDialogTopicGetter>(Skyrim.DialogTopic
                        .WICastMagicNonHostileSpellHealingTopic);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new ExistingDialogueTopicFactory(dialogTopic));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
            }
            case "Player casts Stealth spell on NPC": {
                var dialogTopic =
                    context.GetOrAddOverride<DialogTopic, IDialogTopicGetter>(Skyrim.DialogTopic
                        .WICastMagicNonHostileSpellStealthTopic);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new ExistingDialogueTopicFactory(dialogTopic));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
            }
            case "Player casts other non-hostile spell on NPC": {
                var dialogTopic =
                    context.GetOrAddOverride<DialogTopic, IDialogTopicGetter>(Skyrim.DialogTopic
                        .WICastMagicNonHostileSpellWeirdTopic);
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new ExistingDialogueTopicFactory(dialogTopic));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
            }
        }
    }
}
