using System;
namespace DialogueImplementationTool.Dialogue.Responses; 

public class BackToDialogueRemover : IDialogueResponsePostProcessor {
    public void Process(DialogueResponse response) {
        response.Response = response.Response
            .Replace("[back to root]", string.Empty, StringComparison.InvariantCulture)
            .Replace("[back to dialogue]", string.Empty, StringComparison.InvariantCulture);
    }
}
