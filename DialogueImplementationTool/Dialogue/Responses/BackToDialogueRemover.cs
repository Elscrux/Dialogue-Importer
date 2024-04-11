using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Responses;

public sealed partial class BackToDialogueRemover : IDialogueResponsePostProcessor {
    public void Process(DialogueResponse response) {
        response.Response = Regex().Replace(response.Response, string.Empty);
    }

    [GeneratedRegex(@"(?i)\[back to (root|top|main)( level)?( dialogue)?( options)?\]",
        RegexOptions.IgnoreCase,
        "en-DE")]
    private static partial Regex Regex();
}
