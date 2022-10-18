using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Responses; 

public class BracesRemover : IDialogueResponsePostProcessor {
    private static readonly Regex InvalidBracesRegex = new(@"(\(\s*\)|\[\s*\])");
    
    public void Process(DialogueResponse response) {
        var match = InvalidBracesRegex.Match(response.Response);

        var success = match.Success;
        while (success) {
            response.Response = response.Response.Replace(match.Groups[1].Value, string.Empty);
            
            match = match.NextMatch();
            success = match.Success;
        }
    }
}
