using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Model;

[DebuggerDisplay("{ToString()}")]
public class DialogueResponse : DialogueText, IEqualityComparer<DialogueResponse> {
    public string Response {
        get => Text;
        set => Text = value;
    }

    public string FullResponse => FullText;

    public string ScriptNote { get; set; } = string.Empty;
    public Emotion Emotion { get; set; } = Emotion.Neutral;
    public uint EmotionValue { get; set; } = 50;

    public override bool Equals(object? obj) => Equals(obj as DialogueResponse, this);

    public bool Equals(DialogueResponse? x, DialogueResponse? y) {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;

        return x.FullResponse == y.FullResponse
         && x.ScriptNote == y.ScriptNote;
    }

    public override int GetHashCode() => GetHashCode(this);

    public int GetHashCode(DialogueResponse obj) => HashCode.Combine(obj.FullResponse, obj.ScriptNote);
}
