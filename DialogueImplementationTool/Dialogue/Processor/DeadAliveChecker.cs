using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class DeadAliveChecker(IDialogueContext context) : IDialogueTopicInfoProcessor {
    [GeneratedRegex("(?:if )?(.+) is (?:still )?alive")]
    private static partial Regex AliveRegex();

    [GeneratedRegex("(?:if )?(.+) is dead")]
    private static partial Regex DeadRegex();

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            foreach (var note in response.Notes()) {
                var aliveMatch = AliveRegex().Match(note.Text);
                if (aliveMatch.Success) {
                    HandleMatch(aliveMatch, CompareOperator.EqualTo, 0);
                } else {
                    var deadMatch = DeadRegex().Match(note.Text);
                    if (deadMatch.Success) {
                        HandleMatch(deadMatch, CompareOperator.GreaterThanOrEqualTo, 1);
                    }
                }

                void HandleMatch(Match match, CompareOperator compareOperator, float comparisonValue) {
                    var npc = context.SelectRecord<Npc, INpcGetter>(match.Groups[1].Value);
                    var getDeadCount = new ConditionFloat {
                        Data = new GetDeadCountConditionData {
                            Npc = { Link = { FormKey = npc.FormKey } },
                        },
                        CompareOperator = compareOperator,
                        ComparisonValue = comparisonValue,
                    };
                    topicInfo.ExtraConditions.Add(getDeadCount);
                    response.RemoveNote(note);
                }
            }
        }
    }
}
