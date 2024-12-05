namespace DialogueImplementationTool.Parser;

public static class GeneratedDialogueExtension {
    public static void Create(this GeneratedDialogue dialogue) {
        if (dialogue.Topics.Count == 0) return;

        dialogue.Factory.GenerateDialogue(dialogue.Topics);
    }

    public static void Create(this Conversation conversation) {
        foreach (var dialogue in conversation) {
            dialogue.Create();
        }
    }
}
