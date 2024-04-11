using System.Text.RegularExpressions;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Dialogue.Speaker;

public partial interface ISpeaker {
    public FormKey FormKey { get; }
    public string? EditorID { get; }
    public string Name { get; }

    public static string GetSpeakerName(string name) {
        return ReplaceRegex().Replace(name, string.Empty);
    }

    [GeneratedRegex(@"\s+|-")]
    private static partial Regex ReplaceRegex();
}