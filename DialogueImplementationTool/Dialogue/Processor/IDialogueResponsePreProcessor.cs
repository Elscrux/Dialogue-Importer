using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public interface IDialogueResponsePreProcessor {
    public DialogueResponse Process(DialogueResponse response, FormattedText text);
}
