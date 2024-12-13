using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class PickpocketProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.PickpocketCombat
            or DialogTopic.SubtypeEnum.PickpocketNC
            or DialogTopic.SubtypeEnum.PickpocketTopic;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Player seems like they're trying to pickpocket":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 50
                );

                break;
            case "Bystanding NPC comments":
                yield return new IsActorAVictimConditionData().ToConditionFloat(comparisonValue: 0);

                break;
            case "NPC cares":
                yield return new IsActorAVictimConditionData().ToConditionFloat();

                break;
            case "NPC doesn't care":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 10
                );

                break;
        }
    }
}
