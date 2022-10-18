namespace DialogueImplementationTool.Dialogue.Responses; 

public class Trimmer : IDialogueResponsePostProcessor {
    public void Process(DialogueResponse response) {
        response.Response = response.Response.Trim();
    }
}
