using System;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.Skyrim;
using WeatherType = DialogueImplementationTool.Dialogue.Model.WeatherType;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class WeatherProcessor : IGenericDialogueProcessor {
    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        if (genericDialogue.Weather is null) return;

        var condition = GetCondition(genericDialogue.Weather.Value);
        topicInfo.ExtraConditions.Add(condition);
    }

    private static Condition GetCondition(WeatherType weatherType) {
        return weatherType switch {
            WeatherType.Rainy => new IsRainingConditionData().ToConditionFloat(),
            WeatherType.Pleasant => new IsPleasantConditionData().ToConditionFloat(),
            WeatherType.Snowy => new IsSnowingConditionData().ToConditionFloat(),
            WeatherType.Cloudy => new IsCloudyConditionData().ToConditionFloat(),
            _ => throw new ArgumentOutOfRangeException(nameof(weatherType), weatherType, null)
        };
    }
}
