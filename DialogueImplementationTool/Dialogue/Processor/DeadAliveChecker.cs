using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class DeadAliveChecker(IDialogueContext context) : IDialogueTopicProcessor {
    [GeneratedRegex("(?:if )?(.+) is (?:still )?alive")]
    private static partial Regex AliveRegex();

    [GeneratedRegex("(?:if )?(.+) is dead")]
    private static partial Regex DeadRegex();

    public void Process(DialogueTopic topic) {
        for (var topicInfoIndex = 0; topicInfoIndex < topic.TopicInfos.Count; topicInfoIndex++) {
            var topicInfo = topic.TopicInfos[topicInfoIndex];

            // Handle prompt notes
            foreach (var note in topicInfo.Prompt.Notes()) {
                var aliveMatch = AliveRegex().Match(note.Text);
                Condition condition;
                if (aliveMatch.Success) {
                    condition = GetCondition(aliveMatch, CompareOperator.EqualTo, 0);
                } else {
                    var deadMatch = DeadRegex().Match(note.Text);
                    if (!deadMatch.Success) continue;

                    condition = GetCondition(deadMatch, CompareOperator.GreaterThanOrEqualTo, 1);
                }

                // Apply to topic
                topicInfo.ExtraConditions.Add(condition);
                topicInfo.Prompt.RemoveNote(note);
            }

            // Handle response notes
            for (var i = 0; i < topicInfo.Responses.Count; i++) {
                var response = topicInfo.Responses[i];
                var responseNotes = response.Notes();

                // Find notes referencing alive or dead conditions and create conditions
                var conditions = new List<Condition>();
                var notes = responseNotes
                    .Where(note => {
                        var aliveMatch = AliveRegex().Match(note.Text);
                        if (aliveMatch.Success) {
                            conditions.Add(GetCondition(aliveMatch, CompareOperator.EqualTo, 0));
                            return true;
                        }

                        var deadMatch = DeadRegex().Match(note.Text);
                        if (deadMatch.Success) {
                            conditions.Add(GetCondition(deadMatch, CompareOperator.GreaterThanOrEqualTo, 1));
                            return true;
                        }

                        return false;
                    })
                    .ToList();

                if (notes.Count == 0) continue;

                // Split off response
                var splitOffTopicInfo = new DialogueTopicInfo {
                    Speaker = topicInfo.Speaker,
                    Responses = [response],
                };
                var (_, newTopicInfo) = topicInfo.SplitOffDialogue(splitOffTopicInfo);

                // Add lower priority empty topic info that skips ahead in case the condition is not met
                // Add empty topic to current list if the end was split off, otherwise add it to the split off topic
                if (Equals(newTopicInfo.Responses[0], topicInfo.Responses[0])) {
                    var emptyTopic = topicInfo.CopyWith([new DialogueResponse()]);
                    emptyTopic.MakeSharedInfo();
                    topic.TopicInfos.Insert(topicInfoIndex + 1, emptyTopic);
                } else {
                    var nextTopic = topicInfo.Links[0].TopicInfos;
                    // Only add empty topic if there are other responses to link to
                    if (nextTopic[0].Links.Count > 0) {
                        var emptyTopic = nextTopic[0].CopyWith([new DialogueResponse()]);
                        emptyTopic.MakeSharedInfo();
                        nextTopic.Add(emptyTopic);
                    }
                }

                // Apply conditions to new topic
                newTopicInfo.ExtraConditions.AddRange(conditions);

                // Remove notes from response
                foreach (var note in notes) {
                    response.RemoveNote(note);
                }
            }
        }

        Condition GetCondition(Match match, CompareOperator compareOperator, float comparisonValue) {
            var npc = context.SelectRecord<Npc, INpcGetter>(match.Groups[1].Value);
            var getDeadCount = new ConditionFloat {
                Data = new GetDeadCountConditionData {
                    Npc = { Link = { FormKey = npc.FormKey } },
                },
                CompareOperator = compareOperator,
                ComparisonValue = comparisonValue,
            };
            return getDeadCount;
        }
    }
}
