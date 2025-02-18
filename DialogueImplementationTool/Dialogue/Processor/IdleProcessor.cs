using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class IdleProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype) =>
        subtype is DialogTopic.SubtypeEnum.Idle;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Noises, nonverbal":
            case "Idle bark":
                topicInfo.ResetHours = 0.1f;

                yield return new IsMovingConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 2);

                break;
        }
    }
}
