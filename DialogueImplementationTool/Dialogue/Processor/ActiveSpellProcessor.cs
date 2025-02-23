using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class ActiveSpellProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.PlayerCastProjectileSpell;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Player is seen with an active flame spell":
                yield return new SpellHasKeywordConditionData {
                    RunOnType = Condition.RunOnType.Reference,
                    Reference = { FormKey = Skyrim.PlayerRef.FormKey },
                    Keyword = { Link = { FormKey = Skyrim.Keyword.MagicDamageFire.FormKey } },
                }.ToConditionFloat();

                break;

            case "Player is seen with an active frost spell":
                yield return new SpellHasKeywordConditionData {
                    RunOnType = Condition.RunOnType.Reference,
                    Reference = { FormKey = Skyrim.PlayerRef.FormKey },
                    Keyword = { Link = { FormKey = Skyrim.Keyword.MagicDamageFrost.FormKey } },
                }.ToConditionFloat();

                break;
        }
    }
}
