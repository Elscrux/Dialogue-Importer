using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class SceneProcessor(IDialogueContext context) : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Scene;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        var voiceType = GenericMetaData.GetVoiceType(topicInfo.MetaData);

        switch (description) {
            case "Player drops an item (comment about littering)":
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData, new WIRemoveItemTrashQuestFactory(context));
                GenericMetaData.SetGenericDialogTopicFactory(topicInfo.MetaData, new SceneWithOneDialogTopicFactory(context));
                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } },
                }.ToConditionFloat();

                break;
        }
    }
}
