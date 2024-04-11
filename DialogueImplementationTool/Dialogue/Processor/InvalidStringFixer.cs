using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class InvalidStringFixer : IDialogueResponsePreProcessor {
    public DialogueResponse Process(DialogueResponse response, FormattedText text) {
        var fixedResponse = text.Text;
        foreach (var (invalid, fix) in InvalidString.InvalidStrings) {
            fixedResponse = fixedResponse.Replace(invalid, fix);
        }

        return response with { Response = fixedResponse };
    }
}
