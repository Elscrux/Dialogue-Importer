using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class SceneResponseProcessor : IDialogueTopicInfoProcessor {
    public const string SceneNotePrefix = "SPEAKER=";

    [GeneratedRegex(@"^([^[:]+):\s*(.+)")]
    private static partial Regex SceneLineRegex { get; }

    public static string? GetSpeaker(Note note) {
        if (note.Text.StartsWith(SceneNotePrefix)) {
            return note.Text[SceneNotePrefix.Length..];
        }

        return null;
    }

    public void Process(DialogueTopicInfo topicInfo) {
        if (!topicInfo.Prompt.IsEmpty()) return;

        foreach (var response in topicInfo.Responses) {
            // Extract the speaker and save the name in prompt 
            var match = SceneLineRegex.Match(response.Response);
            if (!match.Success) continue;

            response.StartNotes.Add(new Note { Text = SceneNotePrefix + match.Groups[1].Value });
            response.Response = match.Groups[2].Value;
        }
    }
}
