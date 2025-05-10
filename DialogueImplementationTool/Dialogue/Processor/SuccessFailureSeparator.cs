using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class SuccessFailureSeparator(SkillCheckUtils skillCheckUtils) : IDialogueTopicProcessor {
    [GeneratedRegex("(success|succeeded)", RegexOptions.IgnoreCase)]
    private static partial Regex SuccessRegex { get; }

    [GeneratedRegex("fail(ure)?", RegexOptions.IgnoreCase)]
    private static partial Regex FailureRegex { get; }

    public void Process(DialogueTopic topic) {
        if (topic.TopicInfos is [var firstInfo, var secondInfo]) {
            // Dialogue is already separated in two topic infos
            var (success, failure) = CheckSuccessFailureLines(firstInfo, secondInfo);
            if (success is not null && failure is not null) {
                skillCheckUtils.SetupSkillCheck(success, failure);
                return;
            }
        }

        for (var i = 0; i < topic.TopicInfos.Count; i++) {
            // Dialogue is in one topic info
            var (successInfo, failureInfo) = SeparateSkillCheck(topic, i);
            if (successInfo is null || failureInfo is null) continue;

            skillCheckUtils.SetupSkillCheck(successInfo, failureInfo);
        }
    }

    private static (DialogueTopicInfo? Success, DialogueTopicInfo? Failure) CheckSuccessFailureLines(
        DialogueTopicInfo firstInfo,
        DialogueTopicInfo secondInfo) {
        var successNoteFirstInfo = firstInfo.AllNotes().FirstOrDefault(x => SuccessRegex.IsMatch(x.Text));
        var failureNoteFirstInfo = firstInfo.AllNotes().FirstOrDefault(x => FailureRegex.IsMatch(x.Text));
        var firstIsSuccess = successNoteFirstInfo is not null && failureNoteFirstInfo is null;
        var firstIsFailure = successNoteFirstInfo is null && failureNoteFirstInfo is not null;
        if (firstIsSuccess == firstIsFailure) return (null, null);

        var successNoteSecondInfo = secondInfo.AllNotes().FirstOrDefault(x => SuccessRegex.IsMatch(x.Text));
        var failureNoteSecondInfo = secondInfo.AllNotes().FirstOrDefault(x => FailureRegex.IsMatch(x.Text));
        var secondIsSuccess = successNoteSecondInfo is not null && failureNoteSecondInfo is null;
        var secondIsFailure = successNoteSecondInfo is null && failureNoteSecondInfo is not null;
        if (secondIsSuccess == secondIsFailure) return (null, null);

        var successInfo = firstIsSuccess ? firstInfo : secondInfo;
        var failureInfo = firstIsFailure ? firstInfo : secondInfo;

        successInfo.RemoveNote((firstIsSuccess ? successNoteFirstInfo : successNoteSecondInfo)!);
        failureInfo.RemoveNote((firstIsFailure ? failureNoteFirstInfo : failureNoteSecondInfo)!);
        return (successInfo, failureInfo);
    }

    private static (DialogueTopicInfo? Success, DialogueTopicInfo? Failure) SeparateSkillCheck(DialogueTopic topic, int i) {
        // Check for any success and failure tags
        var topicInfo = topic.TopicInfos[i];
        DialogueResponse? successResponse = null;
        DialogueResponse? failureResponse = null;
        foreach (var dialogueResponse in topicInfo.Responses) {
            if (dialogueResponse.Notes().Any(x => SuccessRegex.IsMatch(x.Text))) {
                successResponse = dialogueResponse;
            } else if (dialogueResponse.Notes().Any(x => FailureRegex.IsMatch(x.Text))) {
                failureResponse = dialogueResponse;
            }
        }

        if (successResponse is null || failureResponse is null) return (null, null);

        var successIndex = topicInfo.Responses.IndexOf(successResponse);
        var failureIndex = topicInfo.Responses.IndexOf(failureResponse);
        if (successIndex == -1 || failureIndex == -1) return (null, null);

        // When there are both a success and failure tag, start processing
        successResponse.RemoveNote(text => SuccessRegex.IsMatch(text));
        failureResponse.RemoveNote(text => FailureRegex.IsMatch(text));
        var successFirst = successIndex < failureIndex;
        var minIndex = successFirst ? successIndex : failureIndex;

        // Separate response ranges
        var responses = topicInfo.Responses.ToArray();
        var successResponses = successFirst ? responses[successIndex..failureIndex] : responses[successIndex..];

        var failureResponses = successFirst ? responses[failureIndex..] : responses[failureIndex..successIndex];

        var previousResponses = successIndex > 0 && failureIndex > 0 ? responses[..minIndex] : null;

        // Insert new topic infos
        topic.TopicInfos.RemoveAt(i);
        var failureInfo = topicInfo.CopyWith(failureResponses.ToList());
        var successInfo = topicInfo.CopyWith(successResponses.ToList());
        if (previousResponses is null) {
            // Have success and failure topic infos
            topic.TopicInfos.Insert(i, failureInfo);
            topic.TopicInfos.Insert(i, successInfo);
        } else {
            // In case there is previous dialogue, but success and failure options in a next topic
            var previousInfo = topicInfo.CopyWith(previousResponses.ToList());
            topic.TopicInfos.Insert(i, previousInfo);
            previousInfo.Append(new DialogueTopic { TopicInfos = [successInfo, failureInfo] });
        }

        return (successInfo, failureInfo);
    }
}
