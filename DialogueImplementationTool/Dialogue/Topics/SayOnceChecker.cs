using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed partial class SayOnceChecker : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.Responses.Count == 0) return;

        var response = topicInfo.Responses[0];
        var previousResponse = response.Response;

        response.Response = InitialRegex().Replace(response.Response, string.Empty);

        if (response.Response != previousResponse) topicInfo.SayOnce = true;
    }

    [GeneratedRegex(@"\[(initial)( (greeting))?\]", RegexOptions.IgnoreCase, "en-DE")]
    private static partial Regex InitialRegex();
}
