using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class ScriptNotesParser : IDialogueResponseProcessor {
    private static readonly Color OrangeColor = Color.Orange;

    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {

        var scriptNotes = response.Notes()
            .Where(note => note.Colors.Any(color => AreColorsSimilar(color, OrangeColor, 20)))
            .ToList();

        response.ScriptNote = string.Join(' ', scriptNotes.Select(x => x.Text));

        foreach (var scriptNote in scriptNotes) {
            response.RemoveNote(scriptNote);
        }
    }

    private static bool AreColorsSimilar(Color color1, Color color2, float threshold) {
        var h1 = color1.GetHue();
        var h2 = color2.GetHue();

        return Math.Abs(h1 - h2) < threshold;
    }
}
