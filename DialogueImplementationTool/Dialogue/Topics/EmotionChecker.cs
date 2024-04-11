using DialogueImplementationTool.Services;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class EmotionChecker(IEmotionClassifier emotionClassifier) : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            var (emotion, value) = emotionClassifier.Classify(response.Response);
            response.Emotion = emotion;
            response.EmotionValue = value;
        }
    }
}
