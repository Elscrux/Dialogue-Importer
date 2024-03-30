namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class EmotionClassifier : IDialogueTopicPostProcessor {
	public void Process(DialogueTopic topic) {
		if (App.DialogueVM.EmotionClassifier == null) return;

		foreach (var response in topic.Responses) {
			var (emotion, value) = App.DialogueVM.EmotionClassifier.Classify(response.Response);
			response.Emotion = emotion;
			response.EmotionValue = value;
		}
	}
}
