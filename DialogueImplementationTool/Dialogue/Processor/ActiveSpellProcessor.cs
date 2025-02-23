using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class ActiveSpellProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.PlayerCastProjectileSpell;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "Player is seen with an active flame spell":
                yield return PlayerCastsSpellWithKeyword(Skyrim.Keyword.MagicDamageFire);

                break;

            case "Player is seen with an active frost spell":
                yield return PlayerCastsSpellWithKeyword(Skyrim.Keyword.MagicDamageFrost);

                break;
        }

        Condition PlayerCastsSpellWithKeyword(IFormLinkGetter formLink) => new SpellHasKeywordConditionData {
            RunOnType = Condition.RunOnType.Reference,
            Reference = Skyrim.PlayerRef,
            Keyword = { Link = { FormKey = formLink.FormKey } },
        }.ToConditionFloat();
    }
}
