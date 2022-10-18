using System;
using System.Drawing;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Responses;

public class ScriptNotesParser : IDialogueResponsePreProcessor {
    private static readonly Regex InvalidBracesRegex = new(@"\[([^\[\]]*)\]");

    private static readonly Color OrangeColor = Color.Orange; 
    private const int SimilarThreshold = 20; 
    
    public DialogueResponse Process(DialogueResponse response, FormattedText text) {
        if (AreColorsSimilar(text.Color, OrangeColor)) {
            var match = InvalidBracesRegex.Match(text.Text);
            return new DialogueResponse { ScriptNote = match.Success ? match.Groups[1].Value : text.Text };
        }
        
        return response;
    }

    private bool AreColorsSimilar(Color color1, Color color2) {
        return Math.Abs(color1.R - color2.R) < SimilarThreshold
         && Math.Abs(color1.G - color2.G) < SimilarThreshold
         && Math.Abs(color1.B - color2.B) < SimilarThreshold;
    }
}
