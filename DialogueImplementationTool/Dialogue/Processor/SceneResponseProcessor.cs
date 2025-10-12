using System.Collections.Generic;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class SceneResponseProcessor : IDialogueResponseProcessor {
    public const string SceneNotePrefix = "SPEAKER=";

    [GeneratedRegex(@"^([^[:]+):\s*(.+)")]
    private static partial Regex SceneLineRegex { get; }

    public static string? GetSpeaker(Note note) {
        if (note.Text.StartsWith(SceneNotePrefix)) {
            return note.Text[SceneNotePrefix.Length..];
        }

        return null;
    }

    public void Process(DialogueResponse response, IList<FormattedText> textSnippets) {
        // Extract the speaker and save the name in prompt 
        var match = SceneLineRegex.Match(response.Response);
        if (!match.Success) return;

        response.StartNotes.Add(new Note { Text = SceneNotePrefix + match.Groups[1].Value });

        // Remove the speaker from the response text
        var originalLength = response.Response.Length;
        var newLength = match.Groups[2].Value.Length;
        var lengthToTrimFromStart = originalLength - newLength;
        while (lengthToTrimFromStart > 0) {
            if (textSnippets.Count == 0) break;

            var first = textSnippets[0];
            if (first.Text.Length <= lengthToTrimFromStart) {
                lengthToTrimFromStart -= first.Text.Length;
                textSnippets.RemoveAt(0);
            } else {
                textSnippets[0] = first with { Text = first.Text[(lengthToTrimFromStart)..] };
            }
        }

        response.Response = match.Groups[2].Value;
    }
}
