using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class DialogueQuestLockUnlockProcessor(IDialogueContext context) : IConversationProcessor {
    private const string LockFillerPart = "(?:(?:-|all) )";

    // [DONE], [HERE]
    [GeneratedRegex(KeywordUtils.KeywordRegexPart)]
    private static partial Regex KeywordRegex();

    // [DONE], [HERE]
    [GeneratedRegex($"^{KeywordUtils.KeywordRegexPart}$")]
    private static partial Regex OnlyKeywordRegex();

    // [Locked]
    // In combination with a SimpleKeywordRegex this creates a locked keyword
    [GeneratedRegex("(?i)^locked$")]
    private static partial Regex LockedRegex();

    // [Locked]
    // In combination with a SimpleKeywordRegex this creates an unlocked keyword
    [GeneratedRegex("(?i)^unlocked")]
    private static partial Regex UnlockedRegex();

    // [unlocked HERE]
    [GeneratedRegex($"(?i)^locked {LockFillerPart}?(?-i){KeywordUtils.KeywordRegexPart}")]
    private static partial Regex StatusLockedRegex();

    // [locked HERE]
    [GeneratedRegex($"(?i)^locked {LockFillerPart}?(?-i){KeywordUtils.KeywordRegexPart}")]
    private static partial Regex StatusUnlockedRegex();

    // [lock all HERE] [remove HERE]
    [GeneratedRegex($"(?i)^(?:lock(?:s)?|remove(?:s)?) {LockFillerPart}?(?-i){KeywordUtils.KeywordRegexPart}")]
    private static partial Regex ActionLockRegex();

    // [unlock HERE] [add HERE]
    [GeneratedRegex($"(?i)^(?:unlock(?:s)?|add(?:s)?) {LockFillerPart}?(?-i){KeywordUtils.KeywordRegexPart}")]
    private static partial Regex ActionUnlockRegex();

    public void Process(Conversation conversation) {
        var speakerStages = new Dictionary<FormKey, (ushort StartStage, ushort NextStage)>();

         var simplePromptKeywords = conversation.GetAllKeywordTopicInfos(
            OnlyKeywordRegex(),
            info => info.Prompt.StartNotes);

        var simpleResponseKeywords = conversation.GetAllKeywordTopicInfos(
            OnlyKeywordRegex(),
            info => info.Responses is [] ? [] : info.Responses[0].StartNotes);

        var statusLockedKeywords = conversation.GetAllKeywordTopicInfos(
            StatusLockedRegex(),
            info => info.Prompt.StartNotes);

        var statusUnlockedKeywords = conversation.GetAllKeywordTopicInfos(
            StatusUnlockedRegex(),
            info => info.Prompt.StartNotes);

        var actionLockKeywords = conversation.GetAllKeywordTopicInfos(
            ActionLockRegex(),
            info => info.Responses.Count == 0 ? [] : info.Responses[^1].EndNotesAndStartIfResponseEmpty());
        var actionUnlockKeywords = conversation.GetAllKeywordTopicInfos(
            ActionUnlockRegex(),
            info => info.Responses.Count == 0 ? [] : info.Responses[^1].EndNotesAndStartIfResponseEmpty());

        var actionKeywords = actionLockKeywords.Select(x => (Match: x, Lock: true))
            .Concat(actionUnlockKeywords.Select(x => (Match: x, Lock: false)))
            .SelectMany(x => {
                // Check for any keywords in note, to catch something like [unlock HERE, NOW, MERGE]
                return KeywordRegex().Matches(x.Match.Note.Text)
                    .Select(match => match.Groups[1].Value)
                    .Select(keyword => (x.Match with { Keyword = keyword }, x.Lock))
                    .ToList();
            })
            .GroupBy(x => x.Item1.Keyword)
            .ToList();

        foreach (var grouping in actionKeywords) {
            foreach (var isLocking in grouping.Select(x => x.Lock).Distinct()) {
                var keyword = grouping.Key;
                var speaker = grouping.First().Item1.TopicInfo.Speaker;
                var stage = CreateStage(speaker);
                var matchesFound = 0;

                if (isLocking) {
                    foreach (var initiallyUnlockedMatch in statusUnlockedKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Unlocked HERE]
                        initiallyUnlockedMatch.TopicInfo.Prompt.StartNotes.Remove(initiallyUnlockedMatch.Note);
                        initiallyUnlockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(false, stage));
                    }

                    foreach (var initiallyUnlockedMatch in simplePromptKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Locked] [HERE]
                        initiallyUnlockedMatch.TopicInfo.Prompt.StartNotes.Remove(initiallyUnlockedMatch.Note);
                        initiallyUnlockedMatch.TopicInfo.Prompt.StartNotes.RemoveAll(x => UnlockedRegex().IsMatch(x.Text));
                        initiallyUnlockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(false, stage));
                    }

                    foreach (var initiallyUnlockedMatch in simpleResponseKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Locked] [HERE]
                        initiallyUnlockedMatch.TopicInfo.Responses[0].StartNotes.Remove(initiallyUnlockedMatch.Note);
                        initiallyUnlockedMatch.TopicInfo.Responses[0].StartNotes
                            .RemoveAll(x => UnlockedRegex().IsMatch(x.Text));
                        initiallyUnlockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(false, stage));
                    }
                } else {
                    foreach (var initiallyLockedMatch in statusLockedKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Locked HERE]
                        initiallyLockedMatch.TopicInfo.Prompt.StartNotes.Remove(initiallyLockedMatch.Note);
                        initiallyLockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(true, stage));
                    }

                    foreach (var initiallyLockedMatch in simplePromptKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Locked] [HERE]
                        initiallyLockedMatch.TopicInfo.Prompt.StartNotes.Remove(initiallyLockedMatch.Note);
                        initiallyLockedMatch.TopicInfo.Prompt.StartNotes.RemoveAll(x => LockedRegex().IsMatch(x.Text));
                        initiallyLockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(true, stage));
                    }

                    foreach (var initiallyLockedMatch in simpleResponseKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Locked] [HERE]
                        initiallyLockedMatch.TopicInfo.Responses[0].StartNotes.Remove(initiallyLockedMatch.Note);
                        initiallyLockedMatch.TopicInfo.Responses[0].StartNotes
                            .RemoveAll(x => LockedRegex().IsMatch(x.Text));
                        initiallyLockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(true, stage));
                    }
                }

                if (matchesFound == 0) {
                    var lockText = isLocking ? "lock" : "unlock";
                    Console.WriteLine($"Keyword {keyword} is not used to {lockText} any dialogue");
                    continue;
                }

                // Add stage
                var locking = isLocking ? "Lock" : "Unlock";
                var entry = $"{locking} {keyword} for {speaker.Name}";
                var questStage = new QuestStage {
                    Index = stage,
                    LogEntries = [
                        new QuestLogEntry {
                            Entry = entry,
                            Flags = 0,
                        }
                    ]
                };
                context.Quest.Stages.Add(questStage);
                speakerStages[speaker.FormKey] = (speakerStages[speaker.FormKey].StartStage, (ushort) (stage + 1));

                foreach (var (lockMatch, _) in grouping.Distinct()) {
                    // Check for any keywords in note, to catch something like [unlock HERE, NOW, MERGE]
                    // Remove [Lock HERE] or [Unlock HERE]
                    lockMatch.TopicInfo.Responses[^1].RemoveNote(lockMatch.Note);
                    lockMatch.TopicInfo.RemoveRedundantResponses();
                    lockMatch.TopicInfo.Script.StartScriptLines.Add($"GetOwningQuest().SetStage({stage})");
                }
            }
        }

        ushort CreateStage(ISpeaker speaker) {
            // Get stage for keyword to lock
            const ushort speakerRange = 10;
            if (!speakerStages.TryGetValue(speaker.FormKey, out var speakerStage)) {
                ushort currentIndex = 10;
                if (context.Quest.Stages.Count == 0) {
                    speakerStage = (currentIndex, currentIndex);
                    speakerStages[speaker.FormKey] = speakerStage;
                } else {
                    foreach (var stage in context.Quest.Stages.OrderBy(x => x.Index)) {
                        // Check if the current range is free
                        if (stage.Index >= currentIndex + speakerRange) {
                            speakerStage = (currentIndex, currentIndex);
                            speakerStages[speaker.FormKey] = speakerStage;
                            break;
                        }

                        // Skip if the stage range is already taken
                        if (stage.Index >= currentIndex && stage.Index < currentIndex + speakerRange) {
                            currentIndex += speakerRange;
                        }
                    }

                    // If no free-range was found, take first free stage after the last stage
                    if (speakerStage.StartStage == 0) {
                        var nextStage = (ushort) (context.Quest.Stages[^1].Index + speakerRange);
                        nextStage -= (ushort) (nextStage % speakerRange);

                        speakerStage = (nextStage, nextStage);
                        speakerStages[speaker.FormKey] = speakerStage;
                    }
                }
            }

            return speakerStage.NextStage;
        }

        ConditionFloat GetStageDoneCondition(bool isInitiallyLocked, int stage) {
            var getStageDone = new GetStageDoneConditionData {
                Stage = stage,
                SecondUnusedStringParameter = null
            };
            getStageDone.Quest = new FormLinkOrIndex<IQuestGetter>(getStageDone, context.Quest.FormKey);
            return new ConditionFloat {
                Data = getStageDone,
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = isInitiallyLocked ? 1 : 0,
            };
        }
    }
}
