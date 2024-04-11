﻿using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class SuccessFailureSeparator : IDialogueTopicProcessor {
    public void Process(DialogueTopic topic) {
        for (var i = 0; i < topic.TopicInfos.Count; i++) {
            // Check for any success and failure tags
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

            // When there are both a success and failure tag, start processing
            successResponse.Response = SuccessRegex().Replace(successResponse.Response, string.Empty).Trim();
            failureResponse.Response = FailureRegex().Replace(failureResponse.Response, string.Empty).Trim();
            var successFirst = successIndex < failureIndex;
            var minIndex = successFirst ? successIndex : failureIndex;

            // Separate response ranges
            var responses = topicInfo.Responses.ToArray();
            var successResponses = successFirst ? responses[successIndex..failureIndex] : responses[successIndex..];

            var failureResponses = successFirst ? responses[failureIndex..] : responses[failureIndex..successIndex];

            var previousResponses = successIndex > 0 && failureIndex > 0 ? responses[..minIndex] : null;

            // Insert new topic infos
            topic.TopicInfos.RemoveAt(i);
            var failureInfo = topicInfo with { Responses = failureResponses.ToList() };
            var successInfo = topicInfo with { Responses = successResponses.ToList() };
            if (previousResponses is null) {
                // Have success and failure topic infos
                topic.TopicInfos.Insert(i, failureInfo);
                topic.TopicInfos.Insert(i, successInfo);
            } else {
                // In case there is previous dialogue, but success and failure options in a next topic
                var previousInfo = topicInfo with { Responses = previousResponses.ToList() };
                topic.TopicInfos.Insert(i, previousInfo);
                previousInfo.Append(new DialogueTopic { TopicInfos = [successInfo, failureInfo] });
            }
        }
    }

    [GeneratedRegex(@"\[success\]", RegexOptions.IgnoreCase)]
    private static partial Regex SuccessRegex();

    [GeneratedRegex(@"\[failure\]", RegexOptions.IgnoreCase)]
    private static partial Regex FailureRegex();
}
