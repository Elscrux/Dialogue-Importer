using System;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class QuestStageChecker(IDialogueContext context) : IDialogueTopicInfoProcessor {
    private const string After = "(?:Once|When|After)";
    private const string Before = "(?:Before)";
    private const string IsCompleted = "(?:is (?:reached|complete)|begins|has been reached)";

    [GeneratedRegex($@"^(.+): {After} stage (\d+) {IsCompleted}(?: \(quest granted\))? and {Before} stage (\d+) {IsCompleted}$",
        RegexOptions.IgnoreCase)]
    private static partial Regex GetStageDoneTrueFalseRegex { get; }

    [GeneratedRegex($@"^{After} stage (\d+) of (.+) {IsCompleted}$", RegexOptions.IgnoreCase)]
    private static partial Regex GetStageDoneTrueRegex1 { get; }

    [GeneratedRegex($@"^{After} (.+) stage (\d+)( {IsCompleted})?$", RegexOptions.IgnoreCase)]
    private static partial Regex GetStageDoneTrueRegex2 { get; }

    [GeneratedRegex($@"^{After} (.+) {IsCompleted}$", RegexOptions.IgnoreCase)]
    private static partial Regex GetQuestCompletedTrueRegex { get; }

    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt.StartNotes.RemoveAll(CheckNote);
        topicInfo.Prompt.EndsNotes.RemoveAll(CheckNote);
        foreach (var response in topicInfo.Responses) {
            response.StartNotes.RemoveAll(CheckNote);
            response.EndsNotes.RemoveAll(CheckNote);
        }

        bool CheckNote(Note note) {
            var match = GetStageDoneTrueFalseRegex.Match(note.Text);
            if (match.Success) {
                var questName = match.Groups[1].Value;
                var quest = TryGetQuest(questName) ?? context.SelectRecordCanBeNull<Quest, IQuestGetter>($"Quest {questName}");
                if (quest is not null) {
                    var afterStage = int.Parse(match.Groups[2].Value);
                    var beforeStage = int.Parse(match.Groups[3].Value);

                    topicInfo.ExtraConditions.Add(new GetStageDoneConditionData {
                        Quest = { Link = { FormKey = quest.FormKey } },
                        Stage = afterStage,
                    }.ToConditionFloat());

                    topicInfo.ExtraConditions.Add(new GetStageDoneConditionData {
                        Quest = { Link = { FormKey = quest.FormKey } },
                        Stage = beforeStage,
                    }.ToConditionFloat(0));

                    return true;
                }
            }

            match = GetStageDoneTrueRegex1.Match(note.Text);
            if (match.Success) {
                var afterStage = int.Parse(match.Groups[1].Value);
                var questName = match.Groups[2].Value;
                var quest = TryGetQuest(questName) ?? context.SelectRecordCanBeNull<Quest, IQuestGetter>($"Quest {questName}");
                if (quest is not null) {
                    topicInfo.ExtraConditions.Add(new GetStageDoneConditionData {
                        Quest = { Link = { FormKey = quest.FormKey } },
                        Stage = afterStage,
                    }.ToConditionFloat());

                    return true;
                }
            }

            match = GetStageDoneTrueRegex2.Match(note.Text);
            if (match.Success) {
                var questName = match.Groups[1].Value;
                var quest = TryGetQuest(questName) ?? context.SelectRecordCanBeNull<Quest, IQuestGetter>($"Quest {questName}");
                if (quest is not null) {
                    var afterStage = int.Parse(match.Groups[2].Value);

                    topicInfo.ExtraConditions.Add(new GetStageDoneConditionData {
                        Quest = { Link = { FormKey = quest.FormKey } },
                        Stage = afterStage,
                    }.ToConditionFloat());

                    return true;
                }
            }

            match = GetQuestCompletedTrueRegex.Match(note.Text);
            if (match.Success) {
                var questName = match.Groups[1].Value;
                var quest = TryGetQuest(questName) ?? context.SelectRecordCanBeNull<Quest, IQuestGetter>($"Quest {questName}");
                if (quest is not null) {
                    topicInfo.ExtraConditions.Add(new GetQuestCompletedConditionData {
                        Quest = { Link = { FormKey = quest.FormKey } },
                    }.ToConditionFloat());

                    return true;
                }
            }

            return false;
        }

        IQuestGetter? TryGetQuest(string questName) {
            var matchingQuests = context.Environment.LinkCache.WinningOverrides<IQuestGetter>()
                .Where(q => q.Name?.String is not null && q.Name.String.Equals(questName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matchingQuests is [var quest]) return quest;

            return null;
        }
    }
}
