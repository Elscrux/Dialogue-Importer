using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class PlayerRaceProcessor : IGenericDialogueProcessor {
    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        var conditions = GetConditions(genericDialogue.PlayerRace).ToList();

        // Remove the last OR flag to prevent the last condition from being OR'd with any next condition coming after
        if (conditions.Count > 0) {
            conditions[^1].Flags.SetFlag(Condition.Flag.OR, false);
        }

        topicInfo.ExtraConditions.AddRange(conditions);
    }

    private static IEnumerable<Condition> GetConditions(string? race) {
        switch (race) {
            case "Human":
                yield return GetCondition(Skyrim.Race.BretonRace);
                yield return GetCondition(Skyrim.Race.BretonRaceVampire);
                yield return GetCondition(Skyrim.Race.ImperialRace);
                yield return GetCondition(Skyrim.Race.ImperialRaceVampire);
                yield return GetCondition(Skyrim.Race.NordRace);
                yield return GetCondition(Skyrim.Race.NordRaceVampire);
                yield return GetCondition(Skyrim.Race.RedguardRace);
                yield return GetCondition(Skyrim.Race.RedguardRaceVampire);

                break;
            case "NOT Human":
                yield return GetNegatedCondition(Skyrim.Race.BretonRace);
                yield return GetNegatedCondition(Skyrim.Race.BretonRaceVampire);
                yield return GetNegatedCondition(Skyrim.Race.ImperialRace);
                yield return GetNegatedCondition(Skyrim.Race.ImperialRaceVampire);
                yield return GetNegatedCondition(Skyrim.Race.NordRace);
                yield return GetNegatedCondition(Skyrim.Race.NordRaceVampire);
                yield return GetNegatedCondition(Skyrim.Race.RedguardRace);
                yield return GetNegatedCondition(Skyrim.Race.RedguardRaceVampire);

                break;
            case "Elf":
                yield return GetCondition(Skyrim.Race.DarkElfRace);
                yield return GetCondition(Skyrim.Race.DarkElfRaceVampire);
                yield return GetCondition(Skyrim.Race.HighElfRace);
                yield return GetCondition(Skyrim.Race.HighElfRaceVampire);
                yield return GetCondition(Skyrim.Race.WoodElfRace);
                yield return GetCondition(Skyrim.Race.WoodElfRaceVampire);

                break;
            case "NOT Elf":
                yield return GetNegatedCondition(Skyrim.Race.DarkElfRace);
                yield return GetNegatedCondition(Skyrim.Race.DarkElfRaceVampire);
                yield return GetNegatedCondition(Skyrim.Race.HighElfRace);
                yield return GetNegatedCondition(Skyrim.Race.HighElfRaceVampire);
                yield return GetNegatedCondition(Skyrim.Race.WoodElfRace);
                yield return GetNegatedCondition(Skyrim.Race.WoodElfRaceVampire);

                break;
            case "Beast":
                yield return GetCondition(Skyrim.Race.ArgonianRace);
                yield return GetCondition(Skyrim.Race.ArgonianRaceVampire);
                yield return GetCondition(Skyrim.Race.KhajiitRace);
                yield return GetCondition(Skyrim.Race.KhajiitRaceVampire);

                break;
            case "NOT Beast":
                yield return GetNegatedCondition(Skyrim.Race.ArgonianRace);
                yield return GetNegatedCondition(Skyrim.Race.ArgonianRaceVampire);
                yield return GetNegatedCondition(Skyrim.Race.KhajiitRace);
                yield return GetNegatedCondition(Skyrim.Race.KhajiitRaceVampire);

                break;
            case "Altmer":
                yield return GetCondition(Skyrim.Race.HighElfRace);
                yield return GetCondition(Skyrim.Race.HighElfRaceVampire);

                break;
            case "NOT Altmer":
                yield return GetNegatedCondition(Skyrim.Race.HighElfRace);
                yield return GetNegatedCondition(Skyrim.Race.HighElfRaceVampire);

                break;
            case "Argonian":
                yield return GetCondition(Skyrim.Race.ArgonianRace);
                yield return GetCondition(Skyrim.Race.ArgonianRaceVampire);

                break;
            case "NOT Argonian":
                yield return GetNegatedCondition(Skyrim.Race.ArgonianRace);
                yield return GetNegatedCondition(Skyrim.Race.ArgonianRaceVampire);

                break;
            case "Bosmer":
                yield return GetCondition(Skyrim.Race.WoodElfRace);
                yield return GetCondition(Skyrim.Race.WoodElfRaceVampire);

                break;
            case "NOT Bosmer":
                yield return GetNegatedCondition(Skyrim.Race.WoodElfRace);
                yield return GetNegatedCondition(Skyrim.Race.WoodElfRaceVampire);

                break;
            case "Breton":
                yield return GetCondition(Skyrim.Race.BretonRace);
                yield return GetCondition(Skyrim.Race.BretonRaceVampire);

                break;
            case "NOT Breton":
                yield return GetNegatedCondition(Skyrim.Race.BretonRace);
                yield return GetNegatedCondition(Skyrim.Race.BretonRaceVampire);

                break;
            case "Dunmer":
                yield return GetCondition(Skyrim.Race.DarkElfRace);
                yield return GetCondition(Skyrim.Race.DarkElfRaceVampire);

                break;
            case "NOT Dunmer":
                yield return GetNegatedCondition(Skyrim.Race.DarkElfRace);
                yield return GetNegatedCondition(Skyrim.Race.DarkElfRaceVampire);

                break;
            case "Orc":
                yield return GetCondition(Skyrim.Race.OrcRace);
                yield return GetCondition(Skyrim.Race.OrcRaceVampire);

                break;
            case "NOT Orc":
                yield return GetNegatedCondition(Skyrim.Race.OrcRace);
                yield return GetNegatedCondition(Skyrim.Race.OrcRaceVampire);

                break;
            case "Khajiit":
                yield return GetCondition(Skyrim.Race.KhajiitRace);
                yield return GetCondition(Skyrim.Race.KhajiitRaceVampire);

                break;
            case "NOT Khajiit":
                yield return GetNegatedCondition(Skyrim.Race.KhajiitRace);
                yield return GetNegatedCondition(Skyrim.Race.KhajiitRaceVampire);

                break;
            case "Imperial":
                yield return GetCondition(Skyrim.Race.ImperialRace);
                yield return GetCondition(Skyrim.Race.ImperialRaceVampire);

                break;
            case "NOT Imperial":
                yield return GetNegatedCondition(Skyrim.Race.ImperialRace);
                yield return GetNegatedCondition(Skyrim.Race.ImperialRaceVampire);

                break;
            case "Redguard":
                yield return GetCondition(Skyrim.Race.RedguardRace);
                yield return GetCondition(Skyrim.Race.RedguardRaceVampire);

                break;
            case "NOT Redguard":
                yield return GetNegatedCondition(Skyrim.Race.RedguardRace);
                yield return GetNegatedCondition(Skyrim.Race.RedguardRaceVampire);

                break;
            case "Nord":
                yield return GetCondition(Skyrim.Race.NordRace);
                yield return GetCondition(Skyrim.Race.NordRaceVampire);

                break;
            case "NOT Nord":
                yield return GetNegatedCondition(Skyrim.Race.NordRace);
                yield return GetNegatedCondition(Skyrim.Race.NordRaceVampire);

                break;
        }

        Condition GetCondition(FormLink<IRaceGetter> raceLink) => new GetPCIsRaceConditionData {
            Race = {
                Link = {
                    FormKey = raceLink.FormKey
                },
            },
        }.ToConditionFloat(comparisonValue: 1, or: true);

        Condition GetNegatedCondition(FormLink<IRaceGetter> raceLink) => new GetPCIsRaceConditionData {
            Race = {
                Link = {
                    FormKey = raceLink.FormKey
                },
            },
        }.ToConditionFloat(comparisonValue: 0, or: false);
    }
}
