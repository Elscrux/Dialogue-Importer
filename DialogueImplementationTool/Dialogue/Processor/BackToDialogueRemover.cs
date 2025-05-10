using System.Collections.Generic;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class BackToDialogueRemover : IDialogueResponseProcessor {
    [GeneratedRegex("(return|back|go) to(( (root|top|main))?( level)? dialog(ue)?( options)?| (root))", RegexOptions.IgnoreCase)]
    private static partial Regex Regex { get; }

    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
        // Nothing to do here, this is the default behavior
        response.RemoveNote(text => Regex.IsMatch(text));
    }
}
