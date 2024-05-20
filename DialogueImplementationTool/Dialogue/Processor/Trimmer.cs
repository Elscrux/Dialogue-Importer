using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class Trimmer : IDialogueResponseProcessor {
    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
        response.Response = response.Response.Trim();
    }
}
