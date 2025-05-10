using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Services;

public sealed class NullEmotionClassifier : IEmotionClassifier {
    public EmotionValue Classify(string text) {
        return new EmotionValue(Emotion.Neutral, 50);
    }
}
