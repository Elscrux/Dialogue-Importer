namespace DialogueImplementationTool.Parser;

public static class GeneratedDialogueExtension {
    public static void Create(this GeneratedDialogue dialogue) {
        dialogue.Factory.Create(dialogue);
    }

    public static void Create(this Conversation conversation) {
        foreach (var dialogue in conversation) {
            dialogue.Create();
        }
    }
}
