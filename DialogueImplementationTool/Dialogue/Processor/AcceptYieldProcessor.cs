using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class AcceptYieldProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.AcceptYield;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "NPC accepting yield when player sheathes weapon":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 50
                );

                break;
        }
    }
}
