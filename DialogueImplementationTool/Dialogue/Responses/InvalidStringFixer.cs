using System.Collections.Generic;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Responses;

public class InvalidStringFixer : IDialoguePreProcessor {
    private static readonly Dictionary<string, string> InvalidStrings = new() {
        {"\r", ""},
        {"’", "'"},
        {"`", "'"},
        {"”", "\""},
        {"…", "..."},
    };
    
    public DialogueResponse Process(DialogueResponse response, FormattedText text) {
        var fixedResponse = text.Text;
        foreach (var (invalid, fix) in InvalidStrings) {
            fixedResponse = fixedResponse.Replace(invalid, fix);
        }
        
        return response with { Response = fixedResponse };
    }
}
