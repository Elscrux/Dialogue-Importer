using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class NormalToCombatProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.NormalToCombat;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Hostiles see player without searching":
                yield return new GetPlayerTeammateConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 35
                );

                break;
            case "Hostiles see player without searching, Vampire":
                yield return new GetPlayerTeammateConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 35
                );
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Faction = { Link = { FormKey = Skyrim.Faction.VampireFaction.FormKey } }
                }.ToConditionFloat();

                break;
            case "Hostiles see player without searching, Werewolf":
                yield return new GetPlayerTeammateConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 35
                );
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Faction = { Link = { FormKey = Skyrim.Faction.WerewolfFaction.FormKey } }
                }.ToConditionFloat();

                break;
        }
    }
}
