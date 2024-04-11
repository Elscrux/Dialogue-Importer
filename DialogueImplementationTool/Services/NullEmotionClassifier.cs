using Mutagen.Bethesda.Skyrim;
using EmotionValue = (Mutagen.Bethesda.Skyrim.Emotion Emotion, uint Value);
namespace DialogueImplementationTool.Services;

public sealed class NullEmotionClassifier : IEmotionClassifier {
    public EmotionValue Classify(string text) {
        return (Emotion.Neutral, 50);
    }
}
