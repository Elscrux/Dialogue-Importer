using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class EmptyBracesRemover : IDialogueResponseProcessor {
    public void Process(DialogueResponse response, IList<FormattedText> textSnippets) {
        response.RemoveNote(text => text.Trim() == string.Empty);
    }
}
