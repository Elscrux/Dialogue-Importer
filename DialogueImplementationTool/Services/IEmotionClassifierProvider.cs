namespace DialogueImplementationTool.Services;

public interface IEmotionClassifierProvider {
    IEmotionClassifier EmotionClassifier { get; }
}
