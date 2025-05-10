using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public class SkillCheckUtils(IDialogueContext context) {
    public void SetupSkillCheck(DialogueTopicInfo successInfo, DialogueTopicInfo? failureInfo = null) {
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
                failureInfo?.Prompt.RemoveNote(note.Text);
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
                failureInfo?.Prompt.RemoveNote(note.Text);
            }

            if (level != -1) {
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
                    successInfo.ExtraConditions.Add(new ConditionFloat {
                        Data = new GetEquippedConditionData {
                            RunOnType = Condition.RunOnType.Reference,
                            Reference = new FormLink<ISkyrimMajorRecordGetter>(Skyrim.PlayerRef.FormKey),
                            ItemOrList = { Link = { FormKey = Skyrim.FormList.TGAmuletofArticulationList.FormKey } },
                        },
                        CompareOperator = CompareOperator.EqualTo,
                        ComparisonValue = 1,
                    });
                }
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
