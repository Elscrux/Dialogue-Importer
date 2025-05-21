using System;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Speaker;

public sealed class AliasSpeaker : ISpeaker, IEquatable<AliasSpeaker> {
    public int AliasIndex { get; set; }
    public string Name { get; }
    public string NameNoSpaces { get; }
    public IFormLinkGetter FormLink { get; }
    public string? EditorID { get; }

    public AliasSpeaker(IFormLinkGetter formLink, string name, int aliasIndex = -1, string? editorId = null) {
        if (!formLink.IsNull && formLink.Type != typeof(INpcGetter)) {
            throw new ArgumentException($"{formLink.FormKey} is {formLink.Type} - Only INpcGetters are supported for AliasSpeakers");
        }

        FormLink = formLink;
        AliasIndex = aliasIndex;
        Name = name;
        NameNoSpaces = ISpeaker.GetSpeakerName(name);
        FormLink = formLink;
        EditorID = editorId;
    }

    public bool Equals(AliasSpeaker? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return AliasIndex == other.AliasIndex
         && NameNoSpaces == other.NameNoSpaces
         && FormLink.Equals(other.FormLink)
         && EditorID == other.EditorID;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not AliasSpeaker other) return false;

        return Equals(other);
    }

    public override int GetHashCode() => HashCode.Combine(FormLink);
}
