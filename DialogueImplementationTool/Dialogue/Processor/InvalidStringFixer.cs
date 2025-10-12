using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class InvalidStringFixer : IDialogueResponseProcessor {
    public void Process(DialogueResponse response, IList<FormattedText> textSnippets) {
        foreach (var (invalid, fix) in InvalidString.InvalidStrings) {
            response.Response = response.Response.Replace(invalid, fix);

            var i = 0;
            while (i < textSnippets.Count) {
                var snippet = textSnippets[i];
                textSnippets[i] = snippet with { Text = snippet.Text.Replace(invalid, fix) };

                if (string.IsNullOrEmpty(textSnippets[i].Text)) {
                    textSnippets.RemoveAt(i);
                } else {
                    i++;
                }
            }
        }
    }
}
