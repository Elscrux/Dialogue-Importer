using System.Collections.Generic;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class SceneResponseProcessor : IDialogueResponseProcessor {
    public const string SceneNotePrefix = "SPEAKER=";

    [GeneratedRegex(@"^([^[:]+):\s*(.+)")]
    private static partial Regex SceneLineRegex();

    public static string? GetSpeaker(Note note) {
        if (note.Text.StartsWith(SceneNotePrefix)) {
            return note.Text[SceneNotePrefix.Length..];
        }

        return null;
    }

    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
        // Extract the speaker and save the name in prompt 
        var match = SceneLineRegex().Match(response.Response);
        if (!match.Success) return;

        response.StartNotes.Add(new Note { Text = SceneNotePrefix + match.Groups[1].Value });
        response.Response = match.Groups[2].Value;
    }
}
