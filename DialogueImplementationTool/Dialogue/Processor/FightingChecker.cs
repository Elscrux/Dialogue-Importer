using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class FightingChecker(IDialogueContext dialogueContext) : IDialogueTopicInfoProcessor {
    [GeneratedRegex("if fighting (.+)", RegexOptions.IgnoreCase)]
    private static partial Regex FightingRegex { get; }

    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt.StartNotes.RemoveAll(CheckNote);
        topicInfo.Prompt.EndsNotes.RemoveAll(CheckNote);
        foreach (var response in topicInfo.Responses) {
            response.StartNotes.RemoveAll(CheckNote);
            response.EndsNotes.RemoveAll(CheckNote);
        }

        bool CheckNote(Note note) {
            var match = FightingRegex.Match(note.Text);
            if (!match.Success) return false;

            List<FormLink<IKeywordGetter>> keywords = [];
            List<FormLink<IRaceGetter>> races = [];
            var enemy = match.Groups[1].Value;
            if (enemy.Contains("dragon", StringComparison.OrdinalIgnoreCase)) {
                keywords.Add(Skyrim.Keyword.ActorTypeDragon);
            }
            if (enemy.Contains("undead", StringComparison.OrdinalIgnoreCase)) {
                keywords.Add(Skyrim.Keyword.ActorTypeUndead);
            }
            if (enemy.Contains("daedra", StringComparison.OrdinalIgnoreCase)) {
                keywords.Add(Skyrim.Keyword.ActorTypeDaedra);
            }
            if (enemy.Contains("ghost", StringComparison.OrdinalIgnoreCase)) {
                keywords.Add(Skyrim.Keyword.ActorTypeGhost);
            }
            if (enemy.Contains("troll", StringComparison.OrdinalIgnoreCase)) {
                keywords.Add(Skyrim.Keyword.ActorTypeTroll);
            }
            if (enemy.Contains("draugr", StringComparison.OrdinalIgnoreCase)) {
                races.Add(Skyrim.Race.DraugrRace);
            }

            var totalConditions = keywords.Count + races.Count;
            // If no enemy types were found automatically, or if there are multiple possible enemy types (ORed)
            if (totalConditions == 0 || enemy.Contains(" or ")) {
                var record = dialogueContext.SelectRecordCanBeNull($"Enemy: {enemy}", typeof(IKeywordGetter), typeof(IRaceGetter));
                if (record is null) return false;
                
                switch (record) {
                    case IKeywordGetter keyword:
                        keywords.Add(keyword.FormKey);
                        break;
                    case IRaceGetter race:
                        races.Add(race.FormKey);
                        break;
                }
                totalConditions = 1;
            }

            foreach (var keyword in keywords) {
                totalConditions--;

                topicInfo.ExtraConditions.Add(
                    new HasKeywordConditionData {
                        Keyword = { Link = { FormKey = keyword.FormKey } },
                        RunOnType = Condition.RunOnType.CombatTarget,
                    }.ToConditionFloat(or: totalConditions > 0)
                );
            }

            foreach (var race in races) {
                totalConditions--;

                topicInfo.ExtraConditions.Add(
                    new GetIsRaceConditionData {
                        Race = { Link = { FormKey = race.FormKey } },
                        RunOnType = Condition.RunOnType.CombatTarget,
                    }.ToConditionFloat(or: totalConditions > 0)
                );
            }

            return true;
        }
    }
}
