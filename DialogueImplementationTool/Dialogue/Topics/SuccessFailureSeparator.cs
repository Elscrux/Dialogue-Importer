using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Responses;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed partial class SuccessFailureSeparator : IDialogueTopicProcessor {
    public void Process(DialogueTopic topic) {
        for (var i = 0; i < topic.TopicInfos.Count; i++) {
            var topicInfo = topic.TopicInfos[i];
            DialogueResponse? successResponse = null;
            DialogueResponse? failureResponse = null;
            foreach (var dialogueResponse in topicInfo.Responses) {
                if (SuccessRegex().IsMatch(dialogueResponse.Response))
                    successResponse = dialogueResponse;
                else if (FailureRegex().IsMatch(dialogueResponse.Response)) failureResponse = dialogueResponse;
            }

            if (successResponse is null || failureResponse is null) return;

            var successIndex = topicInfo.Responses.IndexOf(successResponse);
            var failureIndex = topicInfo.Responses.IndexOf(failureResponse);
            if (successIndex == -1 || failureIndex == -1) return;

            successResponse.Response = SuccessRegex().Replace(successResponse.Response, string.Empty).Trim();
            failureResponse.Response = FailureRegex().Replace(failureResponse.Response, string.Empty).Trim();
            var successFirst = successIndex < failureIndex;
            var minIndex = successFirst ? successIndex : failureIndex;

            var responses = topicInfo.Responses.ToArray();
            var successResponses = successFirst ? responses[successIndex..failureIndex] : responses[successIndex..];

            var failureResponses = successFirst ? responses[failureIndex..] : responses[failureIndex..successIndex];

            var previousResponses = successIndex > 0 && failureIndex > 0 ? responses[..minIndex] : null;

            topic.TopicInfos.RemoveAt(i);
            topic.TopicInfos.Insert(i, topicInfo with { Responses = failureResponses.ToList() });
            topic.TopicInfos.Insert(i, topicInfo with { Responses = successResponses.ToList() });
            if (previousResponses is not null)
                topic.TopicInfos.Insert(i, topicInfo with { Responses = previousResponses.ToList() });
        }
    }

    [GeneratedRegex(@"\[success\]", RegexOptions.IgnoreCase)]
    private static partial Regex SuccessRegex();

    [GeneratedRegex(@"\[failure\]", RegexOptions.IgnoreCase)]
    private static partial Regex FailureRegex();
}
