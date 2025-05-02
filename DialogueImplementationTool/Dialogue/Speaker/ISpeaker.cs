using System.Text.RegularExpressions;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Dialogue.Speaker;

public partial interface ISpeaker {
    IFormLinkGetter FormLink { get; }
    string? EditorID { get; }
    string Name { get; }
    string NameNoSpaces { get; }

    static string GetSpeakerName(string name) {
        return ReplaceRegex.Replace(name, string.Empty);
    }

    [GeneratedRegex(@"[\s-+|']")]
    private static partial Regex ReplaceRegex { get; }
}
