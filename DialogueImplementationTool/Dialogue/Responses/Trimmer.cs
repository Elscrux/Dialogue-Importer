namespace DialogueImplementationTool.Dialogue.Responses; 

public class Trimmer : IDialoguePostProcessor {
    public void Process(DialogueResponse response) {
        response.Response = response.Response.Trim();
    }
}
