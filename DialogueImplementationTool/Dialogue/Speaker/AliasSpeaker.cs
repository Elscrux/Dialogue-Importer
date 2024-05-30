using System;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Dialogue.Speaker;

public sealed class AliasSpeaker(FormKey formKey, string name, int aliasIndex = -1, string? editorId = null)
    : ISpeaker, IEquatable<AliasSpeaker> {
    public int AliasIndex { get; set; } = aliasIndex;
    public string Name { get; } = name;
    public string NameNoSpaces { get; } = ISpeaker.GetSpeakerName(name);
    public FormKey FormKey { get; } = formKey;
    public string? EditorID { get; } = editorId;

    public bool Equals(AliasSpeaker? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return AliasIndex == other.AliasIndex
         && NameNoSpaces == other.NameNoSpaces
         && FormKey.Equals(other.FormKey)
         && EditorID == other.EditorID;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not AliasSpeaker other) return false;

        return Equals(other);
    }

    public override int GetHashCode() => HashCode.Combine(FormKey);
}
