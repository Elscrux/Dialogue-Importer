using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class AssaultProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.Assault or DialogTopic.SubtypeEnum.AssaultNC;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Watching the assault":
                yield return new IsActorAVictimConditionData().ToConditionFloat(comparisonValue: 0);
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                }.ToConditionFloat();

                break;
            case "Watching the assault and not caring":
                yield return new IsActorAVictimConditionData().ToConditionFloat(comparisonValue: 0);

                break;
            case "Being assaulted":
                yield return new IsActorAVictimConditionData().ToConditionFloat(comparisonValue: 1);

                break;
            case "Being assaulted and needing help":
                yield return new IsActorAVictimConditionData().ToConditionFloat(comparisonValue: 1);
                yield return new GetActorValueConditionData {
                    ActorValue = ActorValue.Confidence
                }.ToConditionFloat(
                    compareOperator: CompareOperator.LessThan,
                    comparisonValue: 3
                );

                break;
            case "Being assaulted and fighting for yourself":
                yield return new IsActorAVictimConditionData().ToConditionFloat(comparisonValue: 1);
                yield return new GetActorValueConditionData {
                    ActorValue = ActorValue.Confidence
                }.ToConditionFloat(
                    compareOperator: CompareOperator.GreaterThanOrEqualTo,
                    comparisonValue: 3
                );

                break;
            case "Player attacks dog":
                yield return new GetInFactionConditionData {
                    Faction = { Link = { FormKey = Skyrim.Faction.DogFaction.FormKey } }
                }.ToConditionFloat();

                break;
            case "Player attacks animal":
                yield return new HasKeywordConditionData {
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeAnimal.FormKey } }
                }.ToConditionFloat();

                break;
        }
    }
}
