using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Processor;

public static partial class NoteUtils {
    private const string NotePattern = @"\[([^\]]*)\]";

    [GeneratedRegex($"^{NotePattern}")]
    public static partial Regex StartNoteRegex();

    [GeneratedRegex($"{NotePattern}$")]
    public static partial Regex EndNoteRegex();

}
