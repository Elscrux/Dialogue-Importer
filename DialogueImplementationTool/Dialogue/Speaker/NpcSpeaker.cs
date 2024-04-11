using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Speaker;

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