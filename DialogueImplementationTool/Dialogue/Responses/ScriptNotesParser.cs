using System;
using System.Drawing;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Responses;

public class ScriptNotesParser : IDialogueResponsePreProcessor {
    private static readonly Regex InvalidBracesRegex = new(@"\[([^\[\]]*)\]");

    private static readonly Color OrangeColor = Color.Orange;
    private static readonly Color SimilarThreshold = Color.FromArgb(64, 64, 16);

    public DialogueResponse Process(DialogueResponse response, FormattedText text) {
        if (AreColorsSimilar(text.Color, OrangeColor, SimilarThreshold)) {
            var match = InvalidBracesRegex.Match(text.Text);
            return new DialogueResponse { ScriptNote = match.Success ? match.Groups[1].Value : text.Text };
        }
        
        return response;
    }

    private bool AreColorsSimilar(Color color1, Color color2, Color thresholdColor) {
        return Math.Abs(color1.R - color2.R) < thresholdColor.R
         && Math.Abs(color1.G - color2.G) < thresholdColor.G
         && Math.Abs(color1.B - color2.B) < thresholdColor.B;
    }
}
