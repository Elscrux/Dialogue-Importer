using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class Trimmer : IDialogueResponseProcessor {
    public void Process(DialogueResponse response, IList<FormattedText> textSnippets) {
        response.Response = response.Response.Trim();

        var i = 0;
        while (i < textSnippets.Count) {
            var snippet = textSnippets[i];
            textSnippets[i] = snippet with { Text = snippet.Text.Trim() };

            if (string.IsNullOrEmpty(textSnippets[i].Text)) {
                textSnippets.RemoveAt(i);
            } else {
                i++;
            }
        }
    }
}
