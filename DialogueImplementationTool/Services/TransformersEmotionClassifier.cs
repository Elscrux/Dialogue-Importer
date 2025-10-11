using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using TransformersSharp.Pipelines;
using Classification = ((string Text, Mutagen.Bethesda.Skyrim.Emotion Actual) Emotion, double Score);
namespace DialogueImplementationTool.Services;

public sealed class TransformersEmotionClassifier : IEmotionClassifier {
    private static readonly Emotion[] Emotions = Enum.GetValues<Emotion>();
    private static readonly ConcurrentBag<(TextClassificationPipeline Pipeline, Dictionary<string, EmotionResult> Emotions)> Outliers = [];

    private readonly List<TextClassificationPipeline> _pipelines = [
        TextClassificationPipeline.FromModel("j-hartmann/emotion-english-distilroberta-base"),
        TextClassificationPipeline.FromModel("j-hartmann/emotion-english-roberta-large"),
        TextClassificationPipeline.FromModel("SamLowe/roberta-base-go_emotions"),
        TextClassificationPipeline.FromModel("michellejieli/emotion_text_classifier"),
        TextClassificationPipeline.FromModel("jitesh/emotion-english"),
        TextClassificationPipeline.FromModel("HarshV9/emotion-english-distilroberta-base"),
        TextClassificationPipeline.FromModel("bhadresh-savani/distilbert-base-uncased-emotion"),
    ];

    public EmotionValue Classify(string text) {
        var classification = GetAverageEmotion(_pipelines, text);

        var emotion = classification.Emotion.Actual;
        return emotion == Emotion.Neutral
            ? new EmotionValue(Emotion.Neutral, 50)
            : new EmotionValue(emotion, (uint) classification.Score);
    }

    private static Classification GetAverageEmotion(IEnumerable<TextClassificationPipeline> pipelines, string text) {
        var classifications = pipelines
            .Select(pipeline => {
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
        Dictionary<Emotion, List<((string Text, Emotion Actual) Emotion, double Score, TextClassificationPipeline pipeline)>> dictionary,
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

    private static Classification GetClassification(TextClassificationPipeline pipeline, string text) {
        var result = pipeline.Classify(text);
        var label = result[0].Label;
        var score = result[0].Score;

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
