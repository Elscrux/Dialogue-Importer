using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class GoodbyeProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Goodbye;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Neutral/Base (0)":
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.EqualTo, comparisonValue: 0);

                break;
            case "Negative (-1 to -4)":
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.LessThanOrEqualTo, comparisonValue: -1);

                break;
            case "Positive (1 to 3)":
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.GreaterThanOrEqualTo, comparisonValue: 1);
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.LessThanOrEqualTo, comparisonValue: 3);

                break;
            case "Lover (4)":
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.EqualTo, comparisonValue: 4);

                break;
        }
    }
}
