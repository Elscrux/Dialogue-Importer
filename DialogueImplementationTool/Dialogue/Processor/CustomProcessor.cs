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
        switch (description) {
            case "NPC returns the dropped item to the player":
                var voiceType = GenericMetaData.GetVoiceType(topicInfo.MetaData);

                // Set quest factory
                var questFactory = new WIRemoveItemReturnQuestFactory(context);
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, questFactory);

                // Add the voice type to the list of voice types for the quest
                var voiceTypesList = questFactory.GetVoiceTypesList();
                voiceTypesList.Items.Add(new FormLink<ISkyrimMajorRecordGetter>(voiceType));

                // Add script to return the item to the player
                topicInfo.Script.StartScriptLines.Add(questFactory.GetReturnItemScript());

                // Add conditions
                yield return questFactory.GetIsBystanderAliasCondition();
                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
        }
    }
}
