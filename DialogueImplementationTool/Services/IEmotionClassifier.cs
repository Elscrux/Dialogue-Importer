using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Services;

public sealed record EmotionValue(Emotion Emotion, uint Value);

public interface IEmotionClassifier {
    EmotionValue Classify(string text);
}
