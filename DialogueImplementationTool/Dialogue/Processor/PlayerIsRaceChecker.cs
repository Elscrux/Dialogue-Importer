using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DynamicData;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class PlayerIsRaceChecker : IDialogueTopicInfoProcessor {
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

    [GeneratedRegex($@"player[\w\s]+(?:{MergedRacesRegexPart})")]
    private static partial Regex IsRaceRegex();

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

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var note in topicInfo.AllNotes()) {
            var match = IsRaceRegex().Match(note.Text);
            if (match.Success) {
                var matchingRace = match.Groups.Values.Skip(1).First(x => x.Success);
                var (regular, vampire) = RaceFormKeys[match.Groups.Values.IndexOf(matchingRace)];

                var getIsRace = new GetIsRaceConditionData();
                getIsRace.Race.Link.SetTo(regular);
                topicInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = getIsRace,
                    ComparisonValue = 1,
                    CompareOperator = CompareOperator.EqualTo,
                    Flags = Condition.Flag.OR,
                });

                var getIsRaceVampire = new GetIsRaceConditionData();
                getIsRaceVampire.Race.Link.SetTo(vampire);
                topicInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = getIsRaceVampire,
                    ComparisonValue = 1,
                    CompareOperator = CompareOperator.EqualTo,
                    Flags = Condition.Flag.OR,
                });
            }

            topicInfo.RemoveNote(note);
        }
    }
}
