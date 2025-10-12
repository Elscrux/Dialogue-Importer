using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class InvalidStringFixer : IDialogueResponseProcessor {
    public void Process(DialogueResponse response, IList<FormattedText> textSnippets) {
        foreach (var (invalid, fix) in InvalidString.InvalidStrings) {
            response.Response = response.Response.Replace(invalid, fix);
        }
    }
}
