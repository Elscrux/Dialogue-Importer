using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class GoodbyeChecker : IDialogueTopicInfoProcessor {
    [GeneratedRegex("(exit|end) (dialog|dialogue|conversation|convo)", RegexOptions.IgnoreCase)]
    private static partial Regex GoodbyeRegex();

    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.Responses.Count == 0) return;

        var response = topicInfo.Responses[^1];

        if (response.EndsNotes.RemoveAll(x => GoodbyeRegex().IsMatch(x.Text)) > 0) {
            topicInfo.Goodbye = true;
        }
    }
}
