using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Model;

public sealed record ScriptPropertyName(ScriptProperty ScriptProperty, string ScriptName);

public sealed class DialogueScript : IEquatable<DialogueScript> {
    public List<string> StartScriptLines { get; init; } = [];
    public List<string> EndScriptLines { get; init; } = [];
    public List<ScriptPropertyName> Properties { get; init; } = [];

    public bool Equals(DialogueScript? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return StartScriptLines.SequenceEqual(other.StartScriptLines)
         && EndScriptLines.SequenceEqual(other.EndScriptLines)
         && Properties.Equals(other.Properties);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;

        return Equals(obj as DialogueScript);
    }

    public override int GetHashCode() => HashCode.Combine(StartScriptLines, EndScriptLines, Properties);
}
