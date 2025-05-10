using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class AutoFailChecker : IDialogueTopicInfoProcessor {
    [GeneratedRegex("auto(-| )?(fail|failing)", RegexOptions.IgnoreCase)]
    public static partial Regex AutoFailRegex { get; }

    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt.EndsNotes.RemoveAll(n => AutoFailRegex.IsMatch(n.Text));
    }
}
