using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed partial class RandomChecker : IDialogueTopicProcessor {
    public void Process(DialogueTopic topic) {
        for (var i = 0; i < topic.TopicInfos.Count; i++) {
            var topicInfo = topic.TopicInfos[i];
            if (topicInfo.Responses.Count == 0) continue;

            // Check if all responses are random
            if (!topicInfo.Responses.TrueForAll(x => RandomRegex().Match(x.Response).Success)) continue;

            // Remove the random tag from all responses
            foreach (var response in topicInfo.Responses) {
                response.Response = RandomRegex().Replace(response.Response, string.Empty).Trim();
            }

            // Add multiple random topic infos in the response
            topicInfo.Random = true;
            for (var responseIndex = 1; responseIndex < topicInfo.Responses.Count; responseIndex++)
                topic.TopicInfos.Insert(
                    i + responseIndex,
                    topicInfo with {
                        Responses = [topicInfo.Responses[responseIndex]],
                    });

            topicInfo.Responses.RemoveRange(1, topicInfo.Responses.Count - 1);
        }
    }

    [GeneratedRegex(@"\[(random)(ized)?\]", RegexOptions.IgnoreCase, "en-DE")]
    private static partial Regex RandomRegex();
}
