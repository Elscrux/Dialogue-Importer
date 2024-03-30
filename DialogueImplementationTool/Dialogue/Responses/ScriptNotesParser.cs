using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Responses;

// todo this doesn't work when the script note is split across multiple formatted text parts (like "[" "script note" "]") - implement this in another step to work with this properly
public sealed class ScriptNotesParser : IDialogueResponsePreProcessor {
	private static readonly Regex InvalidBracesRegex = new(@"\[([^\[\]]*)\]");

	private static readonly Color OrangeColor = Color.Orange;
	private static readonly Color SimilarThreshold = Color.FromArgb(64, 64, 16);

	public DialogueResponse Process(DialogueResponse response, FormattedText text) {
		if (AreColorsSimilar(text.Color, OrangeColor, SimilarThreshold)) {
			var newScriptNotePart = string.Join(' ', InvalidBracesRegex.Matches(text.Text).Select(m => m.Groups[1].Value));
			return new DialogueResponse { ScriptNote = response.ScriptNote + newScriptNotePart };
		}

		return response;
	}

	private bool AreColorsSimilar(Color color1, Color color2, Color thresholdColor) {
		return Math.Abs(color1.R - color2.R) < thresholdColor.R
		 && Math.Abs(color1.G - color2.G) < thresholdColor.G
		 && Math.Abs(color1.B - color2.B) < thresholdColor.B;
	}
}
