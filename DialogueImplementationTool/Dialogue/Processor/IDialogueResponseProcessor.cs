using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public interface IDialogueResponseProcessor {
    void Process(DialogueResponse response, IList<FormattedText> textSnippets);
}
