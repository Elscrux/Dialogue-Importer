using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class ObserveCombatProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.ObserveCombat;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "NPC observes deadly combat or brawl":
                yield return new IsMovingConditionData().ToConditionFloat(comparisonValue: 0);

                break;
            case "NPC observes deadly combat or brawl and runs away":
                yield return new IsMovingConditionData().ToConditionFloat();

                break;
        }
    }
}
