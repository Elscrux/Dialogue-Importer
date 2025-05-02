using System;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class DispositionChecker : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.Responses.Count == 0) return;

        topicInfo.Responses[0].StartNotes.RemoveAll(CheckNote);

        bool CheckNote(Note note) {
            var match = DispositionRegex.Match(note.Text);
            if (match.Success) {
                var compareOperator = match.Groups[1].Value switch {
                    ">" => CompareOperator.GreaterThan,
                    "<" => CompareOperator.LessThan,
                    "=" => CompareOperator.EqualTo,
                    ">=" => CompareOperator.GreaterThanOrEqualTo,
                    "<=" => CompareOperator.LessThanOrEqualTo,
                    _ => throw new InvalidOperationException(),
                };
                var value = int.Parse(match.Groups[2].Value);
                topicInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = new GetRelationshipRankConditionData {
                        TargetNpc = {
                            Link = {
                                FormKey = Skyrim.PlayerRef.FormKey
                            }
                        },
                    },
                    CompareOperator = compareOperator,
                    ComparisonValue = value,
                });
                return true;
            }

            if (DispositionPositiveRegex.IsMatch(note.Text)) {
                topicInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = new GetRelationshipRankConditionData {
                        TargetNpc = {
                            Link = {
                                FormKey = Skyrim.PlayerRef.FormKey
                            }
                        },
                    },
                    CompareOperator = CompareOperator.GreaterThan,
                    ComparisonValue = 0,
                });
                return true;
            }

            if (DispositionNegativeRegex.IsMatch(note.Text)) {
                topicInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = new GetRelationshipRankConditionData {
                        TargetNpc = {
                            Link = {
                                FormKey = Skyrim.PlayerRef.FormKey
                            }
                        },
                    },
                    CompareOperator = CompareOperator.LessThan,
                    ComparisonValue = 0,
                });
                return true;
            }

            return false;
        }
    }

    [GeneratedRegex("disposition (>|<|=|>=|<=) (\\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex DispositionRegex { get; }

    [GeneratedRegex("disposition positive|positive disposition", RegexOptions.IgnoreCase)]
    private static partial Regex DispositionPositiveRegex { get; }

    [GeneratedRegex("disposition negative|negative disposition", RegexOptions.IgnoreCase)]
    private static partial Regex DispositionNegativeRegex { get; }
}
