using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class SayOnceChecker : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.Responses.Count == 0) return;

        var response = topicInfo.Responses[0];
        var regex = InitialRegex();

        if (regex.Match(response.Response) is not { Success: true }) return;

        topicInfo.SayOnce = true;
        response.Response = regex
            .Replace(response.Response, string.Empty)
            .Trim();
    }

    [GeneratedRegex(@"\[(initial)( (greeting))?\]", RegexOptions.IgnoreCase, "en-DE")]
    private static partial Regex InitialRegex();
}
