using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class Trimmer : IDialogueResponsePostProcessor {
    public void Process(DialogueResponse response) {
        response.Response = response.Response.Trim();
    }
}
