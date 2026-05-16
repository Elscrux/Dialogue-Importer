using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

/// <summary>
/// Process gold-related notes like "player has X gold", "player has >= X gold", "X gold added", or "XX gold removed"
/// </summary>
public sealed partial class GoldChecker : IDialogueTopicProcessor {
    // Regex for gold condition notes (captures compare operator and amount)
    [GeneratedRegex(
        $@"^(?:if\s+)?(?:player|pc)(?:\s+has)?\s*{CompareOperatorExtension.CompareOperatorRegex}?\s*(?<amount>\d+)\s*gold$",
        RegexOptions.IgnoreCase)]
    public static partial Regex GoldConditionRegex { get; }

    [GeneratedRegex("^success$", RegexOptions.IgnoreCase)]
    public static partial Regex SuccessRegex { get; }

    [GeneratedRegex("^failure$", RegexOptions.IgnoreCase)]
    public static partial Regex FailureRegex { get; }

    [GeneratedRegex(@"(\d+) gold added", RegexOptions.IgnoreCase)]
    public static partial Regex GoldAddedRegex { get; }

    [GeneratedRegex(@"(\d+) gold removed", RegexOptions.IgnoreCase)]
    public static partial Regex GoldRemovedRegex { get; }

    public void Process(DialogueTopic topic) {
        for (var topicInfoIndex = 0; topicInfoIndex < topic.TopicInfos.Count; topicInfoIndex++) {
            var topicInfo = topic.TopicInfos[topicInfoIndex];

            // Handle prompt notes
            List<Condition> promptConditions = [];
            foreach (var note in topicInfo.Prompt.Notes()) {
                var goldConditionMatch = GoldConditionRegex.Match(note.Text);
                if (!goldConditionMatch.Success) continue;

                var condition = GetCondition(goldConditionMatch);

                // Apply to topic
                promptConditions.Add(condition);
                topicInfo.Prompt.RemoveNote(note);
            }

            // Handle response notes
            bool? lastIsSuccess = null;
            var conditionsPerResponse = topicInfo.Responses
                .Select(response => {
                    // Find notes referencing success or failure conditions and create conditions
                    var conditions = new List<Condition>();
                    var scripts = new List<DialogueScript>();

                    var success = 0;
                    var failure = 0;
                    foreach (var note in response.Notes()) {
                        var successMatch = SuccessRegex.Match(note.Text);
                        if (successMatch.Success || note.IsRepeatLast && lastIsSuccess == true) {
                            success++;
                            if (!note.IsRepeatLast) {
                                conditions.AddRange(promptConditions);
                            }
                            response.RemoveNote(note);
                        }

                        var failureMatch = FailureRegex.Match(note.Text);
                        if (failureMatch.Success || note.IsRepeatLast && lastIsSuccess == false) {
                            failure++;
                            if (!note.IsRepeatLast) {
                                conditions.AddRange(promptConditions.Select(ConditionExtension.Negate));
                            }
                            response.RemoveNote(note);
                        }

                        var goldConditionMatch = GoldConditionRegex.Match(note.Text);
                        if (goldConditionMatch.Success) {
                            var comparisonOperator =
                                CompareOperatorExtension.GetCompareOperator(goldConditionMatch.Groups["compare"].Value);
                            if (comparisonOperator is CompareOperator.GreaterThan or CompareOperator.GreaterThanOrEqualTo) {
                                success++;
                            } else if (comparisonOperator is CompareOperator.LessThanOrEqualTo or CompareOperator.LessThan) {
                                failure++;
                            }
                            conditions.Add(GetCondition(goldConditionMatch));
                            response.RemoveNote(note);
                        }

                        var goldAddedMatch = GoldAddedRegex.Match(note.Text);
                        if (goldAddedMatch.Success) {
                            var amount = int.Parse(goldAddedMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                            scripts.Add(new DialogueScript {
                                Properties = [
                                    new ScriptPropertyName(new ScriptObjectProperty {
                                            Name = "Gold",
                                            Flags = ScriptProperty.Flag.Edited,
                                            Object = Skyrim.MiscItem.Gold001,
                                        },
                                        "MiscObject")
                                ],
                                EndScriptLines = [$"Game.GetPlayer().AddItem(Gold, {amount})"],
                            });
                            response.RemoveNote(note);
                        }

                        var goldRemovedMatch = GoldRemovedRegex.Match(note.Text);
                        if (goldRemovedMatch.Success) {
                            var amount = int.Parse(goldRemovedMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                            scripts.Add(new DialogueScript {
                                Properties = [
                                    new ScriptPropertyName(new ScriptObjectProperty {
                                            Name = "Gold",
                                            Flags = ScriptProperty.Flag.Edited,
                                            Object = Skyrim.MiscItem.Gold001,
                                        },
                                        "MiscObject")
                                ],
                                EndScriptLines = [$"Game.GetPlayer().RemoveItem(Gold, {amount})"],
                            });
                            response.RemoveNote(note);
                        }
                    }

                    var isSuccess = (success == 0 && failure == 0)
                        ? lastIsSuccess
                        : success > failure;

                    lastIsSuccess = isSuccess;

                    return (isSuccess, conditions, scripts);
                })
                .ToArray();

            // If no response conditions were encountered, still add prompt conditions
            if (conditionsPerResponse.All(r => r.conditions.Count == 0)) {
                topicInfo.ExtraConditions.AddRange(promptConditions);
                topicInfo.Script.SetTo(conditionsPerResponse.SelectMany(r => r.scripts).ToArray());
                continue;
            }

            var groupAssignments = DialogueTopicInfo.Build(
                conditionsPerResponse,
                response => response.isSuccess,
                response => GetUniqueConditions(response.conditions),
                response => response.scripts);
            topicInfo.SplitOffDialogueGroups(groupAssignments, topic);
        }

        Condition GetCondition(Match match) {
            return new ConditionFloat {
                Data = new GetItemCountConditionData {
                    ItemOrList = { Link = { FormKey = Skyrim.MiscItem.Gold001.FormKey } },
                },
                CompareOperator = CompareOperatorExtension.GetCompareOperator(match.Groups["compare"].Value),
                ComparisonValue = int.Parse(match.Groups["amount"].Value, CultureInfo.InvariantCulture),
            };
        }

        List<Condition> GetUniqueConditions(IEnumerable<Condition> conditions) {
            var uniqueConditions = new List<Condition>();
            foreach (var condition in conditions) {
                if (uniqueConditions.Any(uc => AreConditionsEquivalent(uc, condition))) continue;

                uniqueConditions.Add(condition);
            }

            return uniqueConditions;
        }

        bool AreConditionsEquivalent(Condition a, Condition b) {
            if (a.GetType() != b.GetType()) return false;

            if (a is ConditionFloat aFloat && b is ConditionFloat bFloat) {
                return aFloat.CompareOperator == bFloat.CompareOperator
                 && Math.Abs(aFloat.ComparisonValue - bFloat.ComparisonValue) < 0.1
                 && aFloat.Data is GetItemCountConditionData aData
                 && bFloat.Data is GetItemCountConditionData bData
                 && aData.ItemOrList.Link.FormKey == bData.ItemOrList.Link.FormKey;
            }

            throw new InvalidDataException(
                $"Unexpected condition type in gold condition note: {a.GetType().FullName}");
        }
    }
}
