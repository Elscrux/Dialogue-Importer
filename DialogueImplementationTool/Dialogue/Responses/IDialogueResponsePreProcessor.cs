using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Responses;

public interface IDialogueResponsePreProcessor {
	public DialogueResponse Process(DialogueResponse response, FormattedText text);
}
