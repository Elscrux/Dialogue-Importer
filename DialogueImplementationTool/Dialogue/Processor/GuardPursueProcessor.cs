using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class GuardPursueProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype) =>
        subtype is DialogTopic.SubtypeEnum.PursueIdleTopic;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        topicInfo.ResetHours = 0.1f;

        switch (description) {
            case "While guard is pursuing criminal":
                yield return NullCondition;

                break;
            case "While guard is arresting criminal":
                yield return new GetArrestingActorConditionData().ToConditionFloat();

                break;
        }
    }
}
