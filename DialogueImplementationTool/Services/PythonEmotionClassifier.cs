using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;
using Noggog;
using Python.Runtime;
using Classification = ((string Text, Mutagen.Bethesda.Skyrim.Emotion Actual) Emotion, float Score);
namespace DialogueImplementationTool.Services;

public sealed class PythonEmotionClassifier : IDisposable, IEmotionClassifier {
    private static readonly Emotion[] Emotions = Enum.GetValues<Emotion>();
    private static readonly ConcurrentBag<(dynamic Pipeline, Dictionary<string, EmotionResult> Emotions)> Outliers = [];
    private readonly Py.GILState _gilState;
    private readonly List<dynamic> _pipelines;

    private readonly Dictionary<string, EmotionValue> _speakerEmotions = LoadSpeakers();

    public PythonEmotionClassifier(string pythonDllPath) {
        PythonDllPath = pythonDllPath;
        Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDllPath);

        PythonEngine.Initialize();
        _gilState = Py.GIL();

        dynamic transformers = Py.Import("transformers");
        _pipelines = [
            transformers.pipeline("text-classification", "j-hartmann/emotion-english-distilroberta-base"),
            transformers.pipeline("text-classification", "j-hartmann/emotion-english-roberta-large"),
            transformers.pipeline("text-classification", "SamLowe/roberta-base-go_emotions"),
            transformers.pipeline("text-classification", "michellejieli/emotion_text_classifier"),
            transformers.pipeline("text-classification", "jitesh/emotion-english"),
            transformers.pipeline("text-classification", "HarshV9/emotion-english-distilroberta-base"),
        ];
    }

    public string PythonDllPath { get; }

    public void Dispose() {
        _gilState.Dispose();
        PythonEngine.Shutdown();
    }

    public EmotionValue Classify(string text) {
        if (_speakerEmotions.TryGetValue(text, out var emotionValue)) return emotionValue;

        var classification = GetAverageEmotion(_pipelines, text);

        var emotion = classification.Emotion.Actual;
        emotionValue = emotion == Emotion.Neutral
            ? new EmotionValue(Emotion.Neutral, 50)
            : new EmotionValue(emotion, (uint) classification.Score);

        _speakerEmotions.Add(text, emotionValue);
        SaveSpeakers();
        return emotionValue;
    }

    private static Dictionary<string, EmotionValue> LoadSpeakers() {
        if (!File.Exists(EmotionsPath)) return new Dictionary<string, EmotionValue>();

        var text = File.ReadAllText(EmotionsPath);
        var emotions = JsonConvert.DeserializeObject<Dictionary<string, EmotionValue>>(text);
        if (emotions is null) return new Dictionary<string, EmotionValue>();

        return emotions;
    }

    private void SaveSpeakers() {
        var text = JsonConvert.SerializeObject(_speakerEmotions);
        var directoryName = Path.GetDirectoryName(EmotionsPath);
        if (directoryName is null) return;

        if (!Directory.Exists(directoryName)) {
            Directory.CreateDirectory(directoryName);
        }

        File.WriteAllText(EmotionsPath, text);
    }

    private static string EmotionsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Emotions", "emotions.cache");

    private static Classification GetAverageEmotion(IEnumerable<dynamic> pipelines, string text) {
        var classifications = pipelines
            .Select(
                pipeline => {
                    Classification classification = GetClassification(pipeline, text);
                    return (classification.Emotion, classification.Score, pipeline);
                })
            .ToList();

        var emotionPerCount = Emotions.ToDictionary(
            emotion => emotion,
            emotion => classifications.Where(x => x.Emotion.Actual == emotion).ToList());
        var maxEmotion = emotionPerCount.MaxBy(x => x.Value.Count);
        var score = maxEmotion.Value.Average(x => x.Score) * 100;

        var amount = maxEmotion.Value.Count == classifications.Count ?
            "all" :
            $"{maxEmotion.Value.Count} of {classifications.Count}";
        if (maxEmotion.Key != Emotion.Neutral)
            Console.WriteLine($"{$"{$"From {amount}",-15} select {maxEmotion.Key} ",-35}\t{(int) score}\t\t\"{text}\"");

        ReportOutliers(emotionPerCount, maxEmotion.Key);

        return ((string.Empty, maxEmotion.Key), score);
    }

    private static void ReportOutliers(
        Dictionary<Emotion, List<((string Text, Emotion Actual) Emotion, float Score, dynamic pipeline)>> dictionary,
        Emotion maxEmotion) {
        foreach (var (key, value) in dictionary) {
            var isMatch = key == maxEmotion;

            foreach (var (emotion, _, pipeline) in value) {
                if (emotion.Actual != key) throw new Exception();

                var x = Outliers.FirstOrDefault(x => x.Pipeline == pipeline);

                if (x.Pipeline is null) {
                    x = (pipeline, []);
                    Outliers.Add(x);
                }

                var emotionResult = x.Emotions.GetOrAdd(
                    emotion.Text,
                    () => new EmotionResult(emotion.Actual, 0, [], 0));

                emotionResult.TotalCount++;
                if (isMatch) {
                    emotionResult.MatchingAmount++;
                } else {
                    var amount = emotionResult.Mismatches.GetOrAdd(maxEmotion, () => 0);
                    emotionResult.Mismatches[maxEmotion] = amount + 1;
                }
            }
        }
    }

    private static Emotion GetEmotion(string emotion) {
        return emotion switch {
            "disapproval" => Emotion.Anger,
            "anger" => Emotion.Anger,
            "Angry" => Emotion.Anger,
            "grumpy" => Emotion.Anger,
            "annoyance" => Emotion.Disgust,
            "disgust" => Emotion.Disgust,
            "Disgusted" => Emotion.Disgust,
            "fear" => Emotion.Fear,
            "Fearful" => Emotion.Fear,
            "nervousness" => Emotion.Fear,
            "admiration" => Emotion.Happy,
            "amusement" => Emotion.Happy,
            "energetic" => Emotion.Happy,
            "approval" => Emotion.Happy,
            "caring" => Emotion.Happy,
            "optimism" => Emotion.Happy,
            "pride" => Emotion.Happy,
            "joy" => Emotion.Happy,
            "relief" => Emotion.Happy,
            "excitement" => Emotion.Happy,
            "gratitude" => Emotion.Happy,
            "love" => Emotion.Happy,
            "Happy" => Emotion.Happy,
            "empathetic" => Emotion.Neutral,
            "others" => Emotion.Neutral,
            "neutral" => Emotion.Neutral,
            "Neutral" => Emotion.Neutral,
            "cheeky" => Emotion.Neutral,
            "curiosity" => Emotion.Neutral,
            "curious" => Emotion.Neutral,
            "Curious to dive deeper" => Emotion.Neutral,
            "desire" => Emotion.Puzzled,
            "embarrassment" => Emotion.Puzzled,
            "puzzled" => Emotion.Puzzled,
            "confuse" => Emotion.Puzzled,
            "guilty" => Emotion.Puzzled,
            "impatient" => Emotion.Puzzled,
            "serious" => Emotion.Puzzled,
            "suspicious" => Emotion.Puzzled,
            "think" => Emotion.Puzzled,
            "disappointment" => Emotion.Sad,
            "whiny" => Emotion.Sad,
            "grief" => Emotion.Sad,
            "remorse" => Emotion.Sad,
            "sadness" => Emotion.Sad,
            "Sad" => Emotion.Sad,
            "realization" => Emotion.Surprise,
            "confusion" => Emotion.Surprise,
            "surprise" => Emotion.Surprise,
            "Surprised" => Emotion.Surprise,
            _ => throw new ArgumentOutOfRangeException(emotion),
        };
    }

    private static Classification GetClassification(dynamic pipeline, string text) {
        var result = pipeline(text);
        string label = result[0]["label"];
        float score = result[0]["score"];

        return ((label, GetEmotion(label)), score);
    }

    private sealed class EmotionResult(
        Emotion emotion,
        int totalCount,
        Dictionary<Emotion, int> mismatches,
        int matchingAmount) {
        public Emotion Emotion { get; } = emotion;
        public int TotalCount { get; set; } = totalCount;
        public Dictionary<Emotion, int> Mismatches { get; } = mismatches;
        public int MatchingAmount { get; set; } = matchingAmount;
    }
}
