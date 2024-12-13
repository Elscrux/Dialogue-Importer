using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TrespassingProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype) =>
        subtype is DialogTopic.SubtypeEnum.Trespass or DialogTopic.SubtypeEnum.TrespassAgainstNC;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Trespassing first caught":
                yield return new IsTrespassingConditionData { RunOnType = Condition.RunOnType.Target }.ToConditionFloat();
                yield return new GetTrespassWarningLevelConditionData().ToConditionFloat(comparisonValue: 0);

                break;
            case "Trespassing second warning":
                yield return new IsTrespassingConditionData { RunOnType = Condition.RunOnType.Target }.ToConditionFloat();
                yield return new GetTrespassWarningLevelConditionData().ToConditionFloat(comparisonValue: 1);

                break;
            case "Trespassing call guard":
                yield return new IsTrespassingConditionData { RunOnType = Condition.RunOnType.Target }.ToConditionFloat();
                yield return new GetTrespassWarningLevelConditionData().ToConditionFloat(comparisonValue: 2);

                break;
            case "Trespassing first caught as guard":
                yield return new IsTrespassingConditionData { RunOnType = Condition.RunOnType.Target }.ToConditionFloat();
                yield return new IsGuardConditionData().ToConditionFloat();
                yield return new GetTrespassWarningLevelConditionData().ToConditionFloat(comparisonValue: 0);

                break;
            case "Trespassing second warning as guard":
                yield return new IsTrespassingConditionData { RunOnType = Condition.RunOnType.Target }.ToConditionFloat();
                yield return new IsGuardConditionData().ToConditionFloat();
                yield return new GetTrespassWarningLevelConditionData().ToConditionFloat(comparisonValue: 1);

                break;
            case "Trespassing arrest player as guard":
                yield return new IsTrespassingConditionData { RunOnType = Condition.RunOnType.Target }.ToConditionFloat();
                yield return new IsGuardConditionData().ToConditionFloat();
                yield return new GetTrespassWarningLevelConditionData().ToConditionFloat(comparisonValue: 2);

                break;
            case "Player is trespassing but NPC doesn't care":
                yield return NullCondition;

                break;
            case "Entering aggro warning radius of npc":
                yield return new IsTrespassingConditionData {
                    RunOnType = Condition.RunOnType.Target
                }.ToConditionFloat(comparisonValue: 0);

                break;
        }
    }
}
