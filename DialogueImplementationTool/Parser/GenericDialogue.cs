using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using WeatherType = DialogueImplementationTool.Dialogue.Model.WeatherType;
namespace DialogueImplementationTool.Parser;

public record GenericDialogue(
    string Description,
    string Category,
    string Line,
    string? ExtraConditions,
    string VaNotes,
    Emotion? Emotion,
    int? EmotionValue,
    WeatherType? Weather,
    string? Time,
    MaleFemaleGender? PlayerSex,
    string? PlayerRace) {
    public GenericDialogue() : this("", "", "", "", "", null, null, null, null, null, null) {}
}
