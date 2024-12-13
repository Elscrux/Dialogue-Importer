using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class CollideActorProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype) =>
        subtype is DialogTopic.SubtypeEnum.ActorCollideWithActor;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Player walks into NPC":
                yield return new IsSmallBumpConditionData().ToConditionFloat();

                break;
            case "Player runs into NPC":
                yield return new IsSmallBumpConditionData().ToConditionFloat(comparisonValue: 0);

                break;
        }
    }
}
