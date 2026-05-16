using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class DeadAliveChecker(IDialogueContext context) : IDialogueTopicProcessor {
    [GeneratedRegex("(?:if )?(.+) is (?:still )?(alive|not dead)")]
    private static partial Regex AliveRegex { get; }

    [GeneratedRegex("(?:if )?(.+) is (dead|not alive)")]
    private static partial Regex DeadRegex { get; }

    [GeneratedRegex(" (and|or) ", RegexOptions.IgnoreCase)]
    private static partial Regex ComplexCondition { get; }

    public void Process(DialogueTopic topic) {
        for (var topicInfoIndex = 0; topicInfoIndex < topic.TopicInfos.Count; topicInfoIndex++) {
            var topicInfo = topic.TopicInfos[topicInfoIndex];

            // Handle prompt notes
            foreach (var note in topicInfo.Prompt.Notes()) {
                if (ComplexCondition.IsMatch(note.Text)) continue;

                var aliveMatch = AliveRegex.Match(note.Text);
                Condition condition;
                if (aliveMatch.Success) {
                    condition = GetCondition(aliveMatch, CompareOperator.EqualTo, 0);
                } else {
                    var deadMatch = DeadRegex.Match(note.Text);
                    if (!deadMatch.Success) continue;

                    condition = GetCondition(deadMatch, CompareOperator.GreaterThanOrEqualTo, 1);
                }

                // Apply to topic
                topicInfo.ExtraConditions.Add(condition);
                topicInfo.Prompt.RemoveNote(note);
            }

            // Handle response notes
            bool? lastIsAlive = null;
            var conditionsPerResponse = topicInfo.Responses
                .Select(response => {
                    // Find notes referencing alive or dead conditions and create conditions
                    var conditions = new List<Condition>();

                    var alive = 0;
                    var dead = 0;
                    foreach (var note in response.Notes()) {
                        if (ComplexCondition.IsMatch(note.Text)) continue;

                        var aliveMatch = AliveRegex.Match(note.Text);
                        if (aliveMatch.Success || note.IsRepeatLast && lastIsAlive == true) {
                            alive++;
                            if (!note.IsRepeatLast) {
                                conditions.Add(GetCondition(aliveMatch, CompareOperator.EqualTo, 0));
                            }
                            response.RemoveNote(note);
                        }

                        var deadMatch = DeadRegex.Match(note.Text);
                        if (deadMatch.Success || note.IsRepeatLast && lastIsAlive == false) {
                            dead++;
                            if (!note.IsRepeatLast) {
                                conditions.Add(GetCondition(deadMatch, CompareOperator.GreaterThanOrEqualTo, 1));
                            }
                            response.RemoveNote(note);
                        }
                    }

                    bool? isAlive = (alive == 0 && dead == 0)
                        ? null
                        : alive > dead;

                    lastIsAlive = isAlive;

                    return (isAlive, conditions);
                })
                .ToArray();

            if (conditionsPerResponse.All(r => r.conditions.Count == 0)) continue;

            var groupAssignments = DialogueTopicInfo.BuildGroupAssignments(
                conditionsPerResponse,
                response => response.isAlive,
                response => response.conditions,
                _ => []);
            topicInfo.SplitOffDialogueGroups(groupAssignments, topic);
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
