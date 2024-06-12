namespace DialogueImplementationTool.Services;

public sealed class NullEmotionClassifierProvider : IEmotionClassifierProvider {
    public IEmotionClassifier EmotionClassifier { get; } = new NullEmotionClassifier();
}
