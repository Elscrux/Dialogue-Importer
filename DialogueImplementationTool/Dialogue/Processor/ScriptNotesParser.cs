using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class ScriptNotesParser : IDialogueResponseProcessor {
    private static readonly Color OrangeColor = Color.Orange;
    private static readonly Color SimilarThreshold = Color.FromArgb(64, 64, 16);

    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
        var scriptNotes = response.Notes()
            .Where(note => note.Colors.Any(color => AreColorsSimilar(color, OrangeColor, SimilarThreshold)))
            .ToList();

        response.ScriptNote = string.Join(' ', scriptNotes.Select(x => x.Text));

        foreach (var scriptNote in scriptNotes) {
            response.RemoveNote(scriptNote);
        }
    }

    private static bool AreColorsSimilar(Color color1, Color color2, Color thresholdColor) {
        return Math.Abs(color1.R - color2.R) < thresholdColor.R
         && Math.Abs(color1.G - color2.G) < thresholdColor.G
         && Math.Abs(color1.B - color2.B) < thresholdColor.B;
    }
}
