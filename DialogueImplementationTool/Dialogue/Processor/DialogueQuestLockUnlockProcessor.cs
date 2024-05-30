using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public partial class DialogueQuestLockUnlockProcessor(IDialogueContext context) : IConversationProcessor {
    private const string KeywordRegexPart = @"([A-Z_\d]+)";

    // [DONE], [HERE]
    [GeneratedRegex(KeywordRegexPart)]
    private static partial Regex KeywordRegex();

    // [DONE], [HERE]
    [GeneratedRegex($"^{KeywordRegexPart}$")]
    private static partial Regex OnlyKeywordRegex();

    // [Locked]
    // In combination with a SimpleKeywordRegex this creates a locked keyword
    [GeneratedRegex("^[Ll]ocked$")]
    private static partial Regex LockedRegex();

    // [unlocked HERE]
    [GeneratedRegex($"^[Ll]ocked (?:- )?{KeywordRegexPart}")]
    private static partial Regex StatusLockedRegex();

    // [locked HERE]
    [GeneratedRegex($"^[Ll]ocked (?:- )?{KeywordRegexPart}")]
    private static partial Regex StatusUnlockedRegex();

    // [lock HERE] [remove HERE]
    [GeneratedRegex($"^(?:[Ll]ock(?:s)?|[Rr]emove(?:s)?) (?:- )?{KeywordRegexPart}")]
    private static partial Regex ActionLockRegex();

    // [unlock HERE] [add HERE]
    [GeneratedRegex($"^(?:[Uu]nlock(?:s)?|[Aa]dd(?:s)?) (?:- )?{KeywordRegexPart}")]
    private static partial Regex ActionUnlockRegex();

    public void Process(Conversation conversation) {
        var speakerStages = new Dictionary<FormKey, (ushort StartStage, ushort NextStage)>();

        var simpleLockedKeywords = conversation.GetAllKeywordTopicInfos(
                OnlyKeywordRegex(),
                info => info.Prompt.StartNotes)
            // Only mind locked keywords
            .Where(x => x.TopicInfo.Prompt.StartNotes.Any(n => LockedRegex().IsMatch(n.Text)))
            .ToList();

        var statusLockedKeywords = conversation.GetAllKeywordTopicInfos(
            StatusLockedRegex(),
            info => info.Prompt.StartNotes);

        var statusUnlockedKeywords = conversation.GetAllKeywordTopicInfos(
            StatusUnlockedRegex(),
            info => info.Prompt.StartNotes);

        var actionLockKeywords = conversation.GetAllKeywordTopicInfos(
            ActionLockRegex(),
            info => info.Responses[^1].EndNotesAndStartIfResponseEmpty());
        var actionUnlockKeywords = conversation.GetAllKeywordTopicInfos(
            ActionUnlockRegex(),
            info => info.Responses[^1].EndNotesAndStartIfResponseEmpty());

        var actionKeywords = actionLockKeywords.Select(x => (Match: x, Lock: true))
            .Concat(actionUnlockKeywords.Select(x => (Match: x, Lock: false)))
            .ToList();

        if (actionKeywords.Any(x => actionKeywords.Any(y => y.Lock != x.Lock && y.Match.Keyword == x.Match.Keyword))) {
            Console.WriteLine("A keyword is locked an unlocked, this is not currently supported.");
            return;
        }

        foreach (var (lockMatch, isLocking) in actionKeywords) {
            var matchesFound = 0;
            var stage = CreateStage(lockMatch, isLocking);

            // Check for any keywords in note, to catch something like [unlock HERE, NOW, MERGE]
            var matches = KeywordRegex().Matches(lockMatch.Note.Text).ToList();
            foreach (var keyword in matches.Select(match => match.Groups[1].Value)) {
                if (isLocking) {
                    foreach (var initiallyUnlockedMatch in statusUnlockedKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Unlocked HERE]
                        initiallyUnlockedMatch.TopicInfo.Prompt.StartNotes.Remove(initiallyUnlockedMatch.Note);
                        initiallyUnlockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(false, stage));
                    }
                } else {
                    foreach (var initiallyLockedMatch in statusLockedKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Locked HERE]
                        initiallyLockedMatch.TopicInfo.Prompt.StartNotes.Remove(initiallyLockedMatch.Note);
                        initiallyLockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(true, stage));
                    }

                    foreach (var initiallyLockedMatch in simpleLockedKeywords.Where(x => x.Keyword == keyword)) {
                        matchesFound++;

                        // Remove [Locked] [HERE]
                        initiallyLockedMatch.TopicInfo.Prompt.StartNotes.Remove(initiallyLockedMatch.Note);
                        initiallyLockedMatch.TopicInfo.Prompt.StartNotes.RemoveAll(x => LockedRegex().IsMatch(x.Text));
                        initiallyLockedMatch.TopicInfo.ExtraConditions.Add(GetStageDoneCondition(true, stage));
                    }
                }
            }

            if (matchesFound == 0) {
                var lockText = isLocking ? "lock" : "unlock";
                Console.WriteLine($"Keyword {lockMatch.Keyword} is not used to {lockText} any dialogue");
                continue;
            }

            // Remove [Lock HERE] or [Unlock HERE]
            lockMatch.TopicInfo.Responses[^1].RemoveNote(lockMatch.Note);
            lockMatch.TopicInfo.RemoveRedundantResponses();
            lockMatch.TopicInfo.Script.ScriptLines.Add($"GetOwningQuest().SetStage({stage})");
        }

        ushort CreateStage(
            (Note Note, string Keyword, DialogueTopic Topic, DialogueTopicInfo TopicInfo) lockMatch,
            bool isLocking) {
            // Get stage for keyword to lock
            const ushort speakerRange = 10;
            if (!speakerStages.TryGetValue(lockMatch.TopicInfo.Speaker.FormKey, out var speakerStage)) {
                ushort currentIndex = 10;
                if (context.Quest.Stages.Count == 0) {
                    speakerStage = (currentIndex, currentIndex);
                    speakerStages[lockMatch.TopicInfo.Speaker.FormKey] = speakerStage;
                } else {
                    foreach (var stage in context.Quest.Stages.OrderBy(x => x.Index)) {
                        // Check if the current range is free
                        if (stage.Index >= currentIndex + speakerRange) {
                            speakerStage = (currentIndex, (ushort) (currentIndex + 1));
                            speakerStages[lockMatch.TopicInfo.Speaker.FormKey] = speakerStage;
                            break;
                        }

                        // Skip if the stage range is already taken
                        if (stage.Index >= currentIndex && stage.Index < currentIndex + speakerRange) {
                            currentIndex += speakerRange;
                        }
                    }

                    // If no free range was found, take first free stage after the last stage
                    if (speakerStage.StartStage == 0) {
                        var nextStage = (ushort) (context.Quest.Stages[^1].Index + speakerRange);
                        nextStage -= (ushort) (nextStage % speakerRange);

                        speakerStage = (nextStage, nextStage);
                        speakerStages[lockMatch.TopicInfo.Speaker.FormKey] = speakerStage;
                    }
                }
            }

            var locking = isLocking ? "Lock" : "Unlock";
            var entry = $"{locking} {lockMatch.Keyword} for {lockMatch.TopicInfo.Speaker.Name}";
            var questStage = new QuestStage {
                Index = speakerStage.NextStage,
                LogEntries = [
                    new QuestLogEntry {
                        Entry = entry,
                        Flags = 0,
                    }
                ]
            };
            context.Quest.Stages.Add(questStage);
            speakerStages[lockMatch.TopicInfo.Speaker.FormKey] =
                (speakerStage.StartStage, (ushort) (speakerStage.NextStage + 1));

            return questStage.Index;
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
