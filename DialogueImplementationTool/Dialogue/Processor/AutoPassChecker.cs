using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class AutoPassChecker(SkillCheckUtils skillCheckUtils) : IDialogueTopicInfoProcessor {
    [GeneratedRegex("auto(-| )?(pass|success|succeed)", RegexOptions.IgnoreCase)]
    public static partial Regex AutoPassRegex { get; }

    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.Prompt.EndsNotes.RemoveAll(n => AutoPassRegex.IsMatch(n.Text)) > 0) {
            skillCheckUtils.SetupSkillCheck(topicInfo);
        }
    }
}
