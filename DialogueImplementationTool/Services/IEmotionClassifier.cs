using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Services;

using EmotionValue = (Emotion Emotion, uint Value);

public interface IEmotionClassifier {
    EmotionValue Classify(string text);
}
