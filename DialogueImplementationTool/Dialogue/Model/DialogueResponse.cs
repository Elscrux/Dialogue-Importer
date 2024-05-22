using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Model;

[DebuggerDisplay("{ToString()}")]
public record DialogueResponse {
    public string Response { get; set; } = string.Empty;

    public string FullResponse {
        get {
            var startNotes = NotesToString(StartNotes, true);
            var endNotes = NotesToString(EndsNotes, false);
            return startNotes + Response + endNotes;
        }
    }

    private static string NotesToString(List<Note> notes, bool start) {
        if (notes.Count == 0) return string.Empty;

        var join = '[' + string.Join("] [", notes.Select(x => x.Text)) + ']';
        return start
            ? join + ' '
            : ' ' + join;
    }

    public string ScriptNote { get; set; } = string.Empty;
    public Emotion Emotion { get; set; } = Emotion.Neutral;
    public uint EmotionValue { get; set; } = 50;

    public List<Note> StartNotes { get; init; } = [];
    public List<Note> EndsNotes { get; init; } = [];

    public IReadOnlyList<Note> Notes() => StartNotes.Concat(EndsNotes).ToList();

    public bool HasNote(Note note) => Notes().Contains(note);
    public bool HasNote(string text) => Notes().Any(note => note.Text.Equals(text, StringComparison.OrdinalIgnoreCase));
    public bool HasNote(Predicate<string> noteMatches) => Notes().Any(note => noteMatches(note.Text));

    public void RemoveNote(Note note) {
        StartNotes.Remove(note);
        EndsNotes.Remove(note);
    }

    public void RemoveNote(string noteText) {
        foreach (var note in Notes()) {
            if (string.Equals(note.Text, noteText, StringComparison.OrdinalIgnoreCase)) {
                RemoveNote(note);
            }
        }
    }

    public bool RemoveNote(Predicate<string> noteMatches) {
        var any = false;
        foreach (var note in Notes()) {
            if (!noteMatches(note.Text)) continue;

            any = true;
            RemoveNote(note);
        }

        return any;
    }

    public virtual bool Equals(DialogueResponse? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return FullResponse == other.FullResponse && ScriptNote == other.ScriptNote;
    }

    public override int GetHashCode() {
        return HashCode.Combine(FullResponse, ScriptNote);
    }

    public override string ToString() {
        return FullResponse;
    }
}
