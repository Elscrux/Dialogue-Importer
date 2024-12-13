using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TauntProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.PowerAttack;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Combat bark":
                yield return new ConditionFloat();

                break;
            case "Fighting a werewolf":
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.CombatTarget,
                    Faction = { Link = { FormKey = Skyrim.Faction.WerewolfFaction.FormKey } }
                }.ToConditionFloat();

                break;
        }
    }
}
