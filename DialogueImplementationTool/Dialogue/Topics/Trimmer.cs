namespace DialogueImplementationTool.Dialogue.Topics; 

public class Trimmer : IDialogueTopicPostProcessor {
    public void Process(DialogueTopic topic) {
        topic.Text = topic.Text.Trim();
        topic.Responses.ForEach(r => r.Response = r.Response.Trim());
    }
}
