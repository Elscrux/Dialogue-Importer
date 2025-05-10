using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DynamicData;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class PlayerIsRaceChecker : IDialogueTopicProcessor {
    private const string ArgonianRegexPart = "(Argonian)";
    private const string AltmerRegexPart = "(Altmer|High Elf|Highelf)";
    private const string BosmerRegexPart = "(Bosmer|Wood Elf|Woodelf)";
    private const string BretonRegexPart = "(Breton)";
    private const string DunmerRegexPart = "(Dunmer|Dark Elf|Darkelf)";
    private const string ImperialRegexPart = "(Imperial)";
    private const string KhajiitRegexPart = "(Khajiit)";
    private const string NordRegexPart = "(Nord)";
    private const string OrcRegexPart = "(Orc|Orsimer)";
    private const string RedguardRegexPart = "(Redguard)";

    private const string MergedRacesRegexPart =
        $"{ArgonianRegexPart}|{AltmerRegexPart}|{BosmerRegexPart}|{BretonRegexPart}|{DunmerRegexPart}"
      + $"|{ImperialRegexPart}|{KhajiitRegexPart}|{NordRegexPart}|{OrcRegexPart}|{RedguardRegexPart}";

    [GeneratedRegex($"(?:if )?(?:[Pp]layer|PC)?.+(?:{MergedRacesRegexPart})")]
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
        foreach (var topicInfo in topic.TopicInfos) {
            foreach (var note in topicInfo.Prompt.Notes()) {
                if (CheckNote(topicInfo, note)) {
                    topicInfo.Prompt.RemoveNote(note);
                }
            }

            foreach (var note in topicInfo.AllNotes()) {
                if (CheckNote(topicInfo, note)) {
                    foreach (var response in topicInfo.Responses) {
                        response.RemoveNote(note);
                    }
                }
            }
        }
    }

    private static bool CheckNote(DialogueTopicInfo topicInfo, Note note) {
        var match = IsRaceRegex.Match(note.Text);
        if (!match.Success) return false;

        var text = note.Text;
        var negated = NegatedRegex.IsMatch(text);
        while (match.Success) {
            var matchingRace = match.Groups.Values.Skip(1).FirstOrDefault(x => x.Success);
            if (matchingRace is null) break;

            var (regular, vampire) = RaceFormKeys[match.Groups.Values.IndexOf(matchingRace)];

            if (negated) {
                AddNegatedConditions(topicInfo, regular, vampire);
            } else {
                AddConditions(topicInfo, regular, vampire);
            }

            text = text.Replace(matchingRace.Value, string.Empty);
            match = IsRaceRegex.Match(text);
        }

        return true;
    }

    private static void AddConditions(DialogueTopicInfo topicInfo, FormKey regular, FormKey vampire) {
        topicInfo.ExtraConditions.Add(new ConditionFloat {
            Data = new GetPCIsRaceConditionData { Race = { Link = { FormKey = regular } } },
            ComparisonValue = 1,
            CompareOperator = CompareOperator.EqualTo,
            Flags = Condition.Flag.OR,
        });

        topicInfo.ExtraConditions.Add(new ConditionFloat {
            Data = new GetPCIsRaceConditionData { Race = { Link = { FormKey = vampire } } },
            ComparisonValue = 1,
            CompareOperator = CompareOperator.EqualTo,
        });
    }

    private static void AddNegatedConditions(DialogueTopicInfo topicInfo, FormKey regular, FormKey vampire) {
        topicInfo.ExtraConditions.Add(new ConditionFloat {
            Data = new GetPCIsRaceConditionData { Race = { Link = { FormKey = regular } } },
            ComparisonValue = 0,
            CompareOperator = CompareOperator.EqualTo,
        });

        topicInfo.ExtraConditions.Add(new ConditionFloat {
            Data = new GetPCIsRaceConditionData { Race = { Link = { FormKey = vampire } } },
            ComparisonValue = 0,
            CompareOperator = CompareOperator.EqualTo,
        });
    }
}
