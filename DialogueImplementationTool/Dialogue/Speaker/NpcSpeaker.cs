using System;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.Dialogue.Speaker;

public sealed class NpcSpeaker : ISpeaker, IEquatable<NpcSpeaker> {
    public IFormLinkGetter FormLink { get; }
    public FormKey FormKey { get; }
    public string? EditorID { get; }
    public string Name { get; }
    public string NameNoSpaces { get; }

    public NpcSpeaker(ILinkCache linkCache, IFormLinkGetter npcFormLink) {
        FormLink = npcFormLink;

        if (linkCache.TryResolve(FormLink, out var recordGetter)) {
            EditorID = recordGetter.EditorID;

            if (linkCache.TryResolve<INamedGetter>(FormLink.FormKey, out var namedGetter)) {
                Name = namedGetter.Name ?? string.Empty;
                NameNoSpaces = ISpeaker.GetSpeakerName(Name);
            } else {
                Name = NameNoSpaces = string.Empty;
            }
        } else {
            EditorID = Name = NameNoSpaces = string.Empty;
        }
    }

    public bool Equals(NpcSpeaker? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return FormLink.Equals(other.FormLink)
         && EditorID == other.EditorID
         && NameNoSpaces == other.NameNoSpaces;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not NpcSpeaker other) return false;

        return Equals(other);
    }

    public override int GetHashCode() => HashCode.Combine(FormLink);
}
