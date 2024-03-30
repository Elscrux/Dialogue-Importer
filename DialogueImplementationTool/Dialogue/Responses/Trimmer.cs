namespace DialogueImplementationTool.Dialogue.Responses;

public sealed class Trimmer : IDialogueResponsePostProcessor {
	public void Process(DialogueResponse response) {
		response.Response = response.Response.Trim();
	}
}
