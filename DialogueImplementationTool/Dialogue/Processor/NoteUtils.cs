using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Processor;

public static partial class NoteUtils {
    private const string NotePattern = @"\[+([^\]]*)\]+";

    [GeneratedRegex(@$"^\s*{NotePattern}")]
    public static partial Regex StartNoteRegex();

    [GeneratedRegex(@$"{NotePattern}\s*$")]
    public static partial Regex EndNoteRegex();
}
