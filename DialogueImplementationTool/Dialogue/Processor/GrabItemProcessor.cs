using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class GrabItemProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.ZKeyObject;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Player grabs item in crosshairs and moves it around":
                yield return new GetRandomPercentConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.LessThanOrEqualTo,
                    comparisonValue: 10
                );

                break;
        }
    }
}
