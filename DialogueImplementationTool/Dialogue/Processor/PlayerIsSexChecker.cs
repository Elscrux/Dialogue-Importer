using System;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class PlayerIsSexChecker : IDialogueTopicProcessor {
    [GeneratedRegex("(?:if|when)(?: player| PC)?(?: is)? (male|female)", RegexOptions.IgnoreCase)]
    private static partial Regex PlayerSexRegex { get; }

    public void Process(DialogueTopic topic) {
        for (var topicIndex = 0; topicIndex < topic.TopicInfos.Count; topicIndex++) {
            var topicInfo = topic.TopicInfos[topicIndex];
            foreach (var note in topicInfo.Prompt.Notes()) {
                var condition = GetCondition(note);

                if (condition is not null) {
                    topicInfo.ExtraConditions.Add(condition);
                    topicInfo.Prompt.RemoveNote(note);
                }
            }

            var lastWasHit = int.MinValue;
            for (var i = 0; i < topicInfo.Responses.Count; i++) {
                var response = topicInfo.Responses[i];
                foreach (var note in response.Notes()) {
                    var condition = GetCondition(note);

                    if (condition is not null) {
                        if (lastWasHit == i - 1) {
                            // Second hit in a row - split off the responses
                            var workingTopic = topic;
                            var workingTopicInfo = topicInfo;

                            var lastResponse = topicInfo.Responses[i - 1];
                            var lastTopicInfo = topicInfo.CopyWith([lastResponse]);

                            if (i > 1) {
                                // If last response is not the first one, we need to split this off from the start of the topic info
                                var lastUntilEndTopicInfo = topicInfo.CopyWith(topicInfo.Responses[(i - 1)..]);
                                (workingTopic, workingTopicInfo) = topicInfo.SplitOffDialogue(lastUntilEndTopicInfo);
                                workingTopic ??= topic;
                            }

                            if (workingTopicInfo.Responses.Count > i + 1) {
                                // If there are more responses after the current one, we need to split off the next response via invisible continue
                                var (_, splitOffTopicInfo) = workingTopicInfo.SplitOffDialogue(lastTopicInfo);

                                var currentTopicInfo = splitOffTopicInfo.CopyWith([response]);
                                currentTopicInfo.ExtraConditions.RemoveWhere(x => x.Data is GetPCIsSexConditionData);
                                currentTopicInfo.ExtraConditions.Add(condition);
                                currentTopicInfo.RemoveNote(note);

                                // Add current topic info to the split off topic
                                workingTopic.TopicInfos.Add(currentTopicInfo);

                                // Remove current topic info from the split off topic info
                                splitOffTopicInfo.Links[0].TopicInfos[0].Responses.RemoveAt(0);
                            } else {
                                // If this is the last response, we just split this into two separate topic infos
                                var currentTopicInfo = workingTopicInfo.CopyWith([workingTopicInfo.Responses[^1]]);
                                currentTopicInfo.ExtraConditions.RemoveWhere(x => x.Data is GetPCIsSexConditionData);
                                currentTopicInfo.ExtraConditions.Add(condition);
                                currentTopicInfo.RemoveNote(note);

                                workingTopicInfo.Responses.RemoveAt(workingTopicInfo.Responses.Count - 1);
                                workingTopic.TopicInfos.Add(currentTopicInfo);
                            }
                        } else {
                            topicInfo.ExtraConditions.Add(condition);
                        }

                        lastWasHit = i;
                        response.RemoveNote(note);
                    }
                }
            }
        }
    }

    public Condition? GetCondition(Note note) {
        var match = PlayerSexRegex.Match(note.Text);
        if (match.Success) {
            var maleFemaleGender = match.Groups[1].Value.ToLower() switch {
                "male" => MaleFemaleGender.Male,
                "female" => MaleFemaleGender.Female,
                _ => throw new InvalidOperationException(),
            };

            return new GetPCIsSexConditionData {
                MaleFemaleGender = maleFemaleGender,
            }.ToConditionFloat();
        }

        return null;
    }
}
