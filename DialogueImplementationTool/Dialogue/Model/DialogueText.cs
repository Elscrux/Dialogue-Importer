using System;
using System.Collections.Generic;
using System.Linq;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Model;

public class DialogueText : IEqualityComparer<DialogueText> {
    public DialogueText() {}

    public DialogueText(DialogueText other) {
        Text = other.Text;
        StartNotes = other.StartNotes.ToList();
        EndsNotes = other.EndsNotes.ToList();
    }

    public string Text { get; set; } = string.Empty;

    public string FullText {
        get {
            var startNotes = NotesToString(StartNotes, true);
            var endNotes = NotesToString(EndsNotes, false);
            return startNotes + Text + endNotes;
        }
    }

    public List<Note> StartNotes { get; set; } = [];
    public List<Note> EndsNotes { get; set; } = [];

    /// <summary>
    /// Has empty response text
    /// </summary>
    public bool IsEmpty() => Text.IsNullOrEmpty();

    public IReadOnlyList<Note> Notes() => StartNotes.Concat(EndsNotes).ToList();

    public IReadOnlyList<Note> EndNotesAndStartIfResponseEmpty() =>
        IsEmpty()
            ? StartNotes.Concat(EndsNotes).ToList()
            : EndsNotes;

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

    private static string NotesToString(List<Note> notes, bool start) {
        if (notes.Count == 0) return string.Empty;

        var join = '[' + string.Join("] [", notes.Select(x => x.Text)) + ']';
        return start
            ? join + ' '
            : ' ' + join;
    }

    public bool Equals(DialogueText? x, DialogueText? y) {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;

        return x.FullText == y.FullText;
    }

    public int GetHashCode(DialogueText obj) => HashCode.Combine(obj.FullText);

    public override string ToString() => FullText;

    public static implicit operator DialogueText(string text) => new() { Text = text };
}
