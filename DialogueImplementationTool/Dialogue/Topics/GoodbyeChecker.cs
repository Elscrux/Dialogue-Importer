using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed partial class GoodbyeChecker : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.Responses.Count == 0) return;

        var response = topicInfo.Responses[^1];
        var previousResponse = response.Response;

        response.Response = GoodbyeRegex().Replace(response.Response, string.Empty);

        if (response.Response != previousResponse) topicInfo.Goodbye = true;
    }

    [GeneratedRegex(@"\[(exit|end) (dialog|dialogue|conversation|convo)\]", RegexOptions.IgnoreCase, "en-DE")]
    private static partial Regex GoodbyeRegex();
}
