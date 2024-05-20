using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class SayOnceChecker : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.Responses.Count == 0) return;

        if (topicInfo.Responses[0].StartNotes.RemoveAll(x => InitialRegex().IsMatch(x.Text)) > 0) {
            topicInfo.SayOnce = true;
        }
    }

    [GeneratedRegex("(initial)( (greeting))?", RegexOptions.IgnoreCase, "en-DE")]
    private static partial Regex InitialRegex();
}
