using System;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Speaker;

public sealed class NpcSpeaker : ISpeaker, IEquatable<NpcSpeaker> {
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

    public bool Equals(NpcSpeaker? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return FormKey.Equals(other.FormKey)
         && EditorID == other.EditorID
         && Name == other.Name;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not NpcSpeaker other) return false;

        return Equals(other);
    }

    public override int GetHashCode() => HashCode.Combine(FormKey, EditorID, Name);
}
