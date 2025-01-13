using System;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Extension;

public static class QuestExtension {
    public static byte[] EventAlias1() => [0x52, 0x31, 0x0, 0x0];
    public static byte[] EventAlias2() => [0x52, 0x32, 0x0, 0x0];

    public static IQuestAliasGetter? GetAlias(this IQuestGetter quest, FormKey npcFormKey) {
        return quest.Aliases.FirstOrDefault(alias => alias.UniqueActor.FormKey == npcFormKey);
    }

    public static IQuestAliasGetter GetOrAddAlias(this IQuest quest, ILinkCache linkCache, FormKey npcFormKey) {
        // Detect existing alias
        var existingAlias = quest.Aliases.Find(alias => alias.UniqueActor.FormKey == npcFormKey);
        if (existingAlias is not null) return existingAlias;

        //Add missing alias
        var npc = linkCache.Resolve<INpcGetter>(npcFormKey);
        var aliasName = npc.ShortName?.String ?? npc.Name?.String ?? npc.EditorID ?? npcFormKey.ToString();

        var alias = new QuestAlias {
            Name = aliasName,
            UniqueActor = new FormLinkNullable<INpcGetter>(npcFormKey),
            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
            ID = Convert.ToUInt32(quest.Aliases.Count),
        };

        quest.Aliases.Add(alias);
        quest.NextAliasID = Convert.ToUInt32(quest.Aliases.Count);

        return alias;
    }

    public static bool IsDialogueQuest(this IQuestGetter quest) {
        return quest.EditorID != null && quest.EditorID.Contains("Dialogue");
    }
}
