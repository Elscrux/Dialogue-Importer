using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class CombatToNormalProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.CombatToNormal;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Fight with humanoid/monster hostiles ends, fighting NPC comments. \"That's the last of them\".":
                yield return new GetPlayerTeammateConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 35
                );
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                }.ToConditionFloat();

                break;
            case "Fight with animal hostiles ends, fighting NPC comments. \"That's the last of them\".":
                yield return new GetPlayerTeammateConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 35
                );
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);

                break;
            case "Player was enemy and died":
                yield return new GetPlayerTeammateConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new GetDeadConditionData {
                    RunOnType = Condition.RunOnType.Reference,
                    Reference = Skyrim.PlayerRef,
                }.ToConditionFloat();

                break;
        }
    }
}
