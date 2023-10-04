using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Responses; 

public class BackToDialogueRemover : IDialogueResponsePostProcessor {
	private readonly Regex _regex = new(@"(?i)\[back to (root|top|main)( level)?( dialogue)?( options)?\]");
    public void Process(DialogueResponse response) {
	    response.Response = _regex.Replace(response.Response, string.Empty);
    }
}
