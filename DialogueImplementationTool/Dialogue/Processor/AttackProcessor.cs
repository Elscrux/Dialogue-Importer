using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class AttackProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Attack;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "NPC attacks generally":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 10
                );

                break;
            case "NPC attacks player":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 10
                );
                yield return new GetIsIDConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Object = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat();

                break;
            case "NPC attacks an NPC who is not the player":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 10
                );
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                }.ToConditionFloat();
                yield return new GetIsIDConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Object = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);

                break;
            case "NPC attacks a creature":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 10
                );
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeCreature.FormKey } }
                }.ToConditionFloat();

                break;
        }
    }
}
