using System.Text.RegularExpressions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public partial interface ISpeaker {
    public FormKey FormKey { get; }
    public string? EditorID { get; }
    public string Name { get; }

    public static string GetSpeakerName(string name) {
        return ReplaceRegex().Replace(name, string.Empty);
    }

    [GeneratedRegex(@"\s+|-")]
    private static partial Regex ReplaceRegex();
}

public sealed class NpcSpeaker : ISpeaker {
    public NpcSpeaker(ILinkCache linkCache, FormKey npcFormKey) {
        FormKey = npcFormKey;

        if (linkCache.TryResolve<INpcGetter>(FormKey, out var npc)) {
            EditorID = npc.EditorID;
            Name = ISpeaker.GetSpeakerName(npc.Name?.String ?? string.Empty);
        } else {
            Name = EditorID = string.Empty;
        }
    }

    public FormKey FormKey { get; }
    public string? EditorID { get; }
    public string Name { get; }
}

public sealed class AliasSpeaker(FormKey formKey, string name, int aliasIndex = -1, string? editorId = null)
    : ISpeaker {
    public int AliasIndex { get; set; } = aliasIndex;
    public string Name { get; } = ISpeaker.GetSpeakerName(name ?? string.Empty);
    public FormKey FormKey { get; } = formKey;
    public string? EditorID { get; } = editorId;
}
