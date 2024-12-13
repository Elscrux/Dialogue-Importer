using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class MurderProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Murder or DialogTopic.SubtypeEnum.MurderNC;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Witnessing murder":
                yield return new IsActorAVictimConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                }.ToConditionFloat();

                break;
            case "Witnessing murder and not caring":
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                }.ToConditionFloat();

                break;
        }
    }
}
