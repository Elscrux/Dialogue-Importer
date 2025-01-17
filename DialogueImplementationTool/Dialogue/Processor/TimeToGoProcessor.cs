using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TimeToGoProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype) =>
        subtype is DialogTopic.SubtypeEnum.TimeToGo;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Player is trespassing and tries to talk to NPC":
                yield return NullCondition;

                break;
        }
    }
}
