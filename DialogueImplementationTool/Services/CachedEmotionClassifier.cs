using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
namespace DialogueImplementationTool.Services;

public sealed class CachedEmotionClassifier(Func<IEmotionClassifier> classifierFactory) : IEmotionClassifier {
    private static string EmotionsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Emotions", "emotions.cache");

    private readonly Dictionary<string, EmotionValue> _cache = Load();
    private IEmotionClassifier? _emotionClassifier;

    public EmotionValue Classify(string text) {
        if (_cache.TryGetValue(text, out var emotionValue)) return emotionValue;

        _emotionClassifier ??= classifierFactory();
        emotionValue = _emotionClassifier.Classify(text);

        _cache.TryAdd(text, emotionValue);
        Save();
        return emotionValue;
    }

    private static Dictionary<string, EmotionValue> Load() {
        if (!File.Exists(EmotionsPath)) return new Dictionary<string, EmotionValue>();

        var text = File.ReadAllText(EmotionsPath);
        var emotions = JsonConvert.DeserializeObject<Dictionary<string, EmotionValue>>(text);
        if (emotions is null) return new Dictionary<string, EmotionValue>();

        return emotions;
    }

    private void Save() {
        var text = JsonConvert.SerializeObject(_cache);
        var directoryName = Path.GetDirectoryName(EmotionsPath);
        if (directoryName is null) return;

        if (!Directory.Exists(directoryName)) {
            Directory.CreateDirectory(directoryName);
        }

        File.WriteAllText(EmotionsPath, text);
    }
}
