using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Processor;

public static partial class NoteUtils {
    [StringSyntax("Regex")] 
    private const string NotePattern = @"\[+([^\]]*)\]+";

    [GeneratedRegex(@$"^\s*{NotePattern}")]
    public static partial Regex StartNoteRegex { get; }

    [GeneratedRegex(@$"{NotePattern}\s*$")]
    public static partial Regex EndNoteRegex { get; }
}
