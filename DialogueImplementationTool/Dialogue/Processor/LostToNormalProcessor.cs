using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class LostToNormalProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.LostToCombat;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "NPC gives up searching for player, combat ends":
                yield return new GetPlayerTeammateConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new GetDeadConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                }.ToConditionFloat(comparisonValue: 0);

                break;
        }
    }
}
