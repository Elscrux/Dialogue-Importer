using System;
namespace DialogueImplementationTool.Dialogue.Responses; 

public class BackToDialogueRemover : IDialogueResponsePostProcessor {
    public void Process(DialogueResponse response) {
        response.Response = response.Response
            .Replace("[back to root]", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("[back to top dialogue]", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("[back to top level dialogue]", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("[back to dialogue options]", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("[back to dialogue]", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
