using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class BeingHitProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Hit;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "NPC is being hit":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 30
                );

                break;
            case "Player hitting friend for the first time":
                yield return new GetFriendHitConditionData().ToConditionFloat(comparisonValue: 1);

                break;
            case "Player hitting friend for the second time":
                yield return new GetFriendHitConditionData().ToConditionFloat(comparisonValue: 2);

                break;
            case "Player hitting friend for the third time, final warning":
                yield return new GetFriendHitConditionData().ToConditionFloat(comparisonValue: 3);

                break;
        }
    }
}
