using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DynamicData;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class PlayerIsRaceChecker : IDialogueTopicProcessor {
    [StringSyntax("Regex")] private const string ArgonianRegexPart = "(Argonian)";
    [StringSyntax("Regex")] private const string AltmerRegexPart = "(Altmer|High Elf|Highelf)";
    [StringSyntax("Regex")] private const string BosmerRegexPart = "(Bosmer|Wood Elf|Woodelf)";
    [StringSyntax("Regex")] private const string BretonRegexPart = "(Breton)";
    [StringSyntax("Regex")] private const string DunmerRegexPart = "(Dunmer|Dark Elf|Darkelf)";
    [StringSyntax("Regex")] private const string ImperialRegexPart = "(Imperial)";
    [StringSyntax("Regex")] private const string KhajiitRegexPart = "(Khajiit)";
    [StringSyntax("Regex")] private const string NordRegexPart = "(Nord)";
    [StringSyntax("Regex")] private const string OrcRegexPart = "(Orc|Orsimer)";
    [StringSyntax("Regex")] private const string RedguardRegexPart = "(Redguard)";

    private const string MergedRacesRegexPart =
        $"{ArgonianRegexPart}|{AltmerRegexPart}|{BosmerRegexPart}|{BretonRegexPart}|{DunmerRegexPart}"
      + $"|{ImperialRegexPart}|{KhajiitRegexPart}|{NordRegexPart}|{OrcRegexPart}|{RedguardRegexPart}";

    [GeneratedRegex(@$"(?:if )?(?:[Pp]layer|PC).+(?:\b{MergedRacesRegexPart}\b)")]
    private static partial Regex IsRaceRegex { get; }

    [GeneratedRegex(@"\bnot\b", RegexOptions.IgnoreCase)]
    private static partial Regex NegatedRegex { get; }

    private static readonly Dictionary<int, (FormKey Regular, FormKey Vampire)> RaceFormKeys = new() {
        { 1, (Skyrim.Race.ArgonianRace.FormKey, Skyrim.Race.ArgonianRaceVampire.FormKey) },
        { 2, (Skyrim.Race.HighElfRace.FormKey, Skyrim.Race.HighElfRaceVampire.FormKey) },
        { 3, (Skyrim.Race.WoodElfRace.FormKey, Skyrim.Race.WoodElfRaceVampire.FormKey) },
        { 4, (Skyrim.Race.BretonRace.FormKey, Skyrim.Race.BretonRaceVampire.FormKey) },
        { 5, (Skyrim.Race.DarkElfRace.FormKey, Skyrim.Race.DarkElfRaceVampire.FormKey) },
        { 6, (Skyrim.Race.ImperialRace.FormKey, Skyrim.Race.ImperialRaceVampire.FormKey) },
        { 7, (Skyrim.Race.KhajiitRace.FormKey, Skyrim.Race.KhajiitRaceVampire.FormKey) },
        { 8, (Skyrim.Race.NordRace.FormKey, Skyrim.Race.NordRaceVampire.FormKey) },
        { 9, (Skyrim.Race.OrcRace.FormKey, Skyrim.Race.OrcRaceVampire.FormKey) },
        { 10, (Skyrim.Race.RedguardRace.FormKey, Skyrim.Race.RedguardRaceVampire.FormKey) },
    };

    public void Process(DialogueTopic topic) {
        for (var topicInfoIndex = 0; topicInfoIndex < topic.TopicInfos.Count; topicInfoIndex++) {
            var topicInfo = topic.TopicInfos[topicInfoIndex];

            // Handle prompt notes - conditions apply to the whole topic info
            foreach (var note in topicInfo.Prompt.Notes()) {
                var (_, conditions) = GetConditions(note);
                if (conditions.Count == 0) continue;

                topicInfo.ExtraConditions.AddRange(conditions);
                topicInfo.Prompt.RemoveNote(note);
            }

            // Handle response notes - conditions apply per response,
            // so responses with different race conditions need to be split into separate topic infos
            var conditionsPerResponse = topicInfo.Responses
                .Select(response => {
                    var conditions = new List<Condition>();
                    var states = new List<string>();
                    foreach (var note in response.Notes()) {
                        var (state, noteConditions) = GetConditions(note);
                        if (noteConditions.Count == 0) continue;

                        conditions.AddRange(noteConditions);
                        states.Add(state);
                        response.RemoveNote(note);
                    }

                    return (State: states.Count == 0 ? null : string.Join("|", states), Conditions: conditions);
                })
                .ToArray();

            if (conditionsPerResponse.All(r => r.Conditions.Count == 0)) continue;

            var groupAssignments = DialogueTopicInfo.BuildGroupAssignments(
                conditionsPerResponse,
                response => response.State,
                response => response.Conditions,
                _ => []);
            topicInfo.SplitOffDialogueGroups(groupAssignments, topic);
        }
    }

    private static (string State, List<Condition> Conditions) GetConditions(Note note) {
        var match = IsRaceRegex.Match(note.Text);
        if (!match.Success) return (string.Empty, []);

        var text = note.Text;
        var negated = NegatedRegex.IsMatch(text);
        var conditions = new List<Condition>();
        var raceNames = new List<string>();
        while (match.Success) {
            var matchingRace = match.Groups.Values.Skip(1).FirstOrDefault(x => x.Success);
            if (matchingRace is null) break;

            var (regular, vampire) = RaceFormKeys[match.Groups.Values.IndexOf(matchingRace)];

            if (negated) {
                AddNegatedConditions(regular, vampire);
            } else {
                AddConditions(regular, vampire);
            }

            raceNames.Add(matchingRace.Value);
            text = text.Replace(matchingRace.Value, string.Empty);
            match = IsRaceRegex.Match(text);
        }

        if (!negated) {
            // Add OR between block of all race conditions
            foreach (var condition in conditions.Take(conditions.Count - 1)) {
                condition.Flags |= Condition.Flag.OR;
            }
        }

        var state = negated
            ? $"NOT {string.Join(",", raceNames)}"
            : string.Join(",", raceNames);
        return (state, conditions);

        void AddConditions(FormKey regular, FormKey vampire) {
            conditions.Add(new ConditionFloat {
                Data = new GetPCIsRaceConditionData { Race = { Link = { FormKey = regular } } },
                ComparisonValue = 1,
                CompareOperator = CompareOperator.EqualTo,
            });

            conditions.Add(new ConditionFloat {
                Data = new GetPCIsRaceConditionData { Race = { Link = { FormKey = vampire } } },
                ComparisonValue = 1,
                CompareOperator = CompareOperator.EqualTo,
            });
        }
        
        void AddNegatedConditions(FormKey regular, FormKey vampire) {
            conditions.Add(new ConditionFloat {
                Data = new GetPCIsRaceConditionData { Race = { Link = { FormKey = regular } } },
                ComparisonValue = 0,
                CompareOperator = CompareOperator.EqualTo,
            });

            conditions.Add(new ConditionFloat {
                Data = new GetPCIsRaceConditionData { Race = { Link = { FormKey = vampire } } },
                ComparisonValue = 0,
                CompareOperator = CompareOperator.EqualTo,
            });
        }
    }
}
