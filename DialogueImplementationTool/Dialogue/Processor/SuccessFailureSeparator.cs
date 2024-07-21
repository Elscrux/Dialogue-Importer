using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class SuccessFailureSeparator(IDialogueContext context) : IDialogueTopicProcessor {
    [GeneratedRegex("(success|succeeded)", RegexOptions.IgnoreCase)]
    private static partial Regex SuccessRegex();

    [GeneratedRegex("fail(ure)?", RegexOptions.IgnoreCase)]
    private static partial Regex FailureRegex();

    public void Process(DialogueTopic topic) {
        if (topic.TopicInfos is [var firstInfo, var secondInfo]) {
            // Dialogue is already separated in two topic infos
            var (success, failure) = CheckSuccessFailureLines(firstInfo, secondInfo);
            if (success is not null && failure is not null) {
                SetupSkillCheck(success, failure);
                return;
            }
        }

        for (var i = 0; i < topic.TopicInfos.Count; i++) {
            // Dialogue is in one topic info
            var (successInfo, failureInfo) = SeparateSkillCheck(topic, i);
            if (successInfo is null || failureInfo is null) continue;

            SetupSkillCheck(successInfo, failureInfo);
        }
    }

    private static (DialogueTopicInfo? Success, DialogueTopicInfo? Failure) CheckSuccessFailureLines(
        DialogueTopicInfo firstInfo,
        DialogueTopicInfo secondInfo) {
        var successNoteFirstInfo = firstInfo.AllNotes().FirstOrDefault(x => SuccessRegex().IsMatch(x.Text));
        var failureNoteFirstInfo = firstInfo.AllNotes().FirstOrDefault(x => FailureRegex().IsMatch(x.Text));
        var firstIsSuccess = successNoteFirstInfo is not null && failureNoteFirstInfo is null;
        var firstIsFailure = successNoteFirstInfo is null && failureNoteFirstInfo is not null;
        if (firstIsSuccess == firstIsFailure) return (null, null);

        var successNoteSecondInfo = secondInfo.AllNotes().FirstOrDefault(x => SuccessRegex().IsMatch(x.Text));
        var failureNoteSecondInfo = secondInfo.AllNotes().FirstOrDefault(x => FailureRegex().IsMatch(x.Text));
        var secondIsSuccess = successNoteSecondInfo is not null && failureNoteSecondInfo is null;
        var secondIsFailure = successNoteSecondInfo is null && failureNoteSecondInfo is not null;
        if (secondIsSuccess == secondIsFailure) return (null, null);

        var successInfo = firstIsSuccess ? firstInfo : secondInfo;
        var failureInfo = firstIsFailure ? firstInfo : secondInfo;

        successInfo.RemoveNote((firstIsSuccess ? successNoteFirstInfo : successNoteSecondInfo)!);
        failureInfo.RemoveNote((firstIsFailure ? failureNoteFirstInfo : failureNoteSecondInfo)!);
        return (successInfo, failureInfo);
    }

    private static (DialogueTopicInfo? Success, DialogueTopicInfo? Failure) SeparateSkillCheck(DialogueTopic topic, int i) {
        // Check for any success and failure tags
        var topicInfo = topic.TopicInfos[i];
        DialogueResponse? successResponse = null;
        DialogueResponse? failureResponse = null;
        foreach (var dialogueResponse in topicInfo.Responses) {
            if (dialogueResponse.Notes().Any(x => SuccessRegex().IsMatch(x.Text))) {
                successResponse = dialogueResponse;
            } else if (dialogueResponse.Notes().Any(x => FailureRegex().IsMatch(x.Text))) {
                failureResponse = dialogueResponse;
            }
        }

        if (successResponse is null || failureResponse is null) return (null, null);

        var successIndex = topicInfo.Responses.IndexOf(successResponse);
        var failureIndex = topicInfo.Responses.IndexOf(failureResponse);
        if (successIndex == -1 || failureIndex == -1) return (null, null);

        // When there are both a success and failure tag, start processing
        successResponse.RemoveNote(text => SuccessRegex().IsMatch(text));
        failureResponse.RemoveNote(text => FailureRegex().IsMatch(text));
        var successFirst = successIndex < failureIndex;
        var minIndex = successFirst ? successIndex : failureIndex;

        // Separate response ranges
        var responses = topicInfo.Responses.ToArray();
        var successResponses = successFirst ? responses[successIndex..failureIndex] : responses[successIndex..];

        var failureResponses = successFirst ? responses[failureIndex..] : responses[failureIndex..successIndex];

        var previousResponses = successIndex > 0 && failureIndex > 0 ? responses[..minIndex] : null;

        // Insert new topic infos
        topic.TopicInfos.RemoveAt(i);
        var failureInfo = topicInfo.CopyWith(failureResponses.ToList());
        var successInfo = topicInfo.CopyWith(successResponses.ToList());
        if (previousResponses is null) {
            // Have success and failure topic infos
            topic.TopicInfos.Insert(i, failureInfo);
            topic.TopicInfos.Insert(i, successInfo);
        } else {
            // In case there is previous dialogue, but success and failure options in a next topic
            var previousInfo = topicInfo.CopyWith(previousResponses.ToList());
            topic.TopicInfos.Insert(i, previousInfo);
            previousInfo.Append(new DialogueTopic { TopicInfos = [successInfo, failureInfo] });
        }

        return (successInfo, failureInfo);
    }

    private void SetupSkillCheck(DialogueTopicInfo successInfo, DialogueTopicInfo failureInfo) {
        var playerText = successInfo.Prompt.Text;
        if (playerText.EndsWith("(Intimidate)")) {
            // Set success condition for intimidate
            successInfo.ExtraConditions.Add(new ConditionFloat {
                Data = new GetIntimidateSuccessConditionData {
                    RunOnType = Condition.RunOnType.Subject,
                },
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1,
            });

            successInfo.Script.StartScriptLines.Add("pFDS.Intimidate(akSpeaker)");
            successInfo.Script.Properties.Add(new ScriptPropertyName(FavorDialogueScriptProperty(), "FavorDialogueScript"));

            // Remove intimidate notes
            foreach (var note in successInfo.Prompt.Notes()) {
                if (!_levelMap.TryGetValue(note.Text, out _)) continue;

                successInfo.Prompt.RemoveNote(note.Text);
                failureInfo.Prompt.RemoveNote(note.Text);
            }
        } else {
            var skillCheck = _skillCheckActorValue.Keys.FirstOrDefault(x => playerText.EndsWith(x));
            if (skillCheck is null) return;

            var actorValue = _skillCheckActorValue[skillCheck];
            var level = -1;

            foreach (var note in successInfo.Prompt.Notes()) {
                if (!_levelMap.TryGetValue(note.Text, out var noteLevel)) continue;

                level = noteLevel;
                successInfo.Prompt.RemoveNote(note.Text);
                failureInfo.Prompt.RemoveNote(note.Text);
            }

            if (level == -1) return;

            var globalFormKey = _actorValueLevels[actorValue][level];

            // Add skill check to success topic info
            var condition = new ConditionGlobal {
                Data = new GetActorValueConditionData {
                    RunOnType = Condition.RunOnType.Reference,
                    Reference = Skyrim.PlayerRef,
                    ActorValue = actorValue,
                },
                CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                ComparisonValue = globalFormKey.ToLink<IGlobalGetter>(),
            };
            successInfo.ExtraConditions.Add(condition);

            if (actorValue == ActorValue.Speech) {
                condition.Flags = condition.Flags.SetFlag(Condition.Flag.OR, true);

                var getEquipped = new GetEquippedConditionData {
                    RunOnType = Condition.RunOnType.Reference,
                    Reference = new FormLink<ISkyrimMajorRecordGetter>(Skyrim.PlayerRef.FormKey),
                };
                getEquipped.ItemOrList.Link.SetTo(Skyrim.FormList.TGAmuletofArticulationList.FormKey);
                successInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = getEquipped,
                    CompareOperator = CompareOperator.EqualTo,
                    ComparisonValue = 1,
                    Flags = Condition.Flag.OR,
                });
            }

            var (line, scriptName) = _actorValueScriptLines[actorValue];
            successInfo.Script.StartScriptLines.Add(line);
            successInfo.Script.Properties.Add(new ScriptPropertyName(FavorDialogueScriptProperty(), scriptName));
        }
    }

    private readonly Dictionary<string, ActorValue> _skillCheckActorValue = new() {
        { "(Persuade)", ActorValue.Speech },
        { "(Illusion)", ActorValue.Illusion },
        { "(Charm)", ActorValue.Illusion },
    };

    private readonly Dictionary<ActorValue, Dictionary<int, FormKey>> _actorValueLevels = new() {
        {
            ActorValue.Speech, new Dictionary<int, FormKey> {
                { 1, Skyrim.Global.SpeechVeryEasy.FormKey },
                { 2, Skyrim.Global.SpeechEasy.FormKey },
                { 3, Skyrim.Global.SpeechAverage.FormKey },
                { 4, Skyrim.Global.SpeechHard.FormKey },
                { 5, Skyrim.Global.SpeechVeryHard.FormKey },
            }
        }, {
            ActorValue.Illusion, new Dictionary<int, FormKey> {
                { 1, FormKey.Factory("001317:BSAssets.esm") },
                { 2, FormKey.Factory("001315:BSAssets.esm") },
                { 3, FormKey.Factory("001314:BSAssets.esm") },
                { 4, FormKey.Factory("001316:BSAssets.esm") },
                { 5, FormKey.Factory("001318:BSAssets.esm") },
            }
        },
    };

    private ScriptProperty FavorDialogueScriptProperty() => new ScriptObjectProperty {
        Name = "pFDS",
        Flags = ScriptProperty.Flag.Edited,
        Object = context.GetFavorDialogueQuest(),
    };

    private readonly Dictionary<ActorValue, (string Line, string ScriptName)> _actorValueScriptLines = new() {
        { ActorValue.Speech, ("pFDS.Persuade(akSpeaker)", "FavorDialogueScript") },
        { ActorValue.Illusion, ("pFDS.Charm(akSpeaker)", "BSKFavorDialogueScript") },
    };

    private readonly Dictionary<string, int> _levelMap = new() {
        // very easy
        { "very easy", 1 },
        { "Novice", 1 },
        { "10", 1 },
        // easy
        { "easy", 2 },
        { "Apprentice", 2 },
        { "25", 2 },
        // average
        { "average", 3 },
        { "medium", 3 },
        { "Adept", 3 },
        { "50", 3 },
        // hard
        { "hard", 4 },
        { "Expert", 4 },
        { "75", 4 },
        // very hard
        { "very hard", 5 },
        { "Master", 5 },
        { "100", 5 },
    };
}
