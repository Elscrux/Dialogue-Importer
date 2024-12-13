using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class CombatToLostProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.CombatToLost;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "NPC loses sight of the player in combat":
                yield return new GetPlayerTeammateConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new IsBleedingOutConditionData().ToConditionFloat(comparisonValue: 0);

                break;
        }
    }
}
