using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Responses;

public sealed partial class BracesRemover : IDialogueResponsePostProcessor {
    public void Process(DialogueResponse response) {
        var match = InvalidBracesRegex().Match(response.Response);

        var success = match.Success;
        while (success) {
            response.Response = response.Response.Replace(match.Groups[1].Value, string.Empty);

            match = match.NextMatch();
            success = match.Success;
        }
    }

    [GeneratedRegex(@"(\(\s*\)|\[\s*\])")]
    private static partial Regex InvalidBracesRegex();
}
