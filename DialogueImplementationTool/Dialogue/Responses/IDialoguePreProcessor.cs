using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Responses;

public interface IDialoguePreProcessor {
    public DialogueResponse Process(DialogueResponse response, FormattedText text);
}
