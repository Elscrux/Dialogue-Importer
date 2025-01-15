using System.Collections.Generic;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class WICommentQuestFactory(
    IDialogueContext context,
    string commentType,
    string description,
    IEnumerable<Condition> extraConditions)
    : IGenericDialogueQuestFactory {
    public const string TopicCommentScript = "(GetOwningQuest() as WICommentScript).Commented()";

    public Quest Create() {
        var questEditorId = $"{context.Prefix}WIComment{commentType}";
        var defaultNpcVoiceTypeList = context.SelectRecord<FormList, IFormListGetter>("Default NPC Voice Types formlist");

        return context.GetOrAddRecord<Quest, IQuestGetter>(
            questEditorId,
            () => new Quest(context.GetNextFormKey(), context.Release) {
                EditorID = questEditorId,
                Priority = 30,
                Filter = context.Quest.Filter,
                DialogConditions = [
                    ..extraConditions,
                    new GetInFactionConditionData {
                        Faction = { Link = { FormKey = Skyrim.Faction.WINeverFillAliasesFaction.FormKey } },
                    }.ToConditionFloat(comparisonValue: 0),
                    new GetAllowWorldInteractionsConditionData().ToConditionFloat(),
                    new ConditionGlobal {
                        Data = new GetGlobalValueConditionData {
                            Global = { Link = { FormKey = Skyrim.Global.GameDaysPassed.FormKey } },

                        },
                        CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                        ComparisonValue = Skyrim.Global.WICommentNextAllowed,
                    },
                    new GetIsVoiceTypeConditionData {
                        VoiceTypeOrList = { Link = { FormKey = defaultNpcVoiceTypeList.FormKey } },
                    }.ToConditionFloat(),
                    new GetDetectedConditionData {
                        TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } },
                    }.ToConditionFloat(),
                ],
                Name = description,
                Flags = Quest.Flag.StartGameEnabled,
                VirtualMachineAdapter = new QuestAdapter {
                    Scripts = [
                        new ScriptEntry {
                            Name = "WICommentScript",
                            Flags = ScriptEntry.Flag.Local,
                            Properties = [
                                new ScriptObjectProperty {
                                    Name = "GameDaysPassed",
                                    Flags = ScriptProperty.Flag.Edited,
                                    Object = Skyrim.Global.GameDaysPassed,
                                },
                                new ScriptObjectProperty {
                                    Name = "WICommentNextAllowed",
                                    Flags = ScriptProperty.Flag.Edited,
                                    Object = Skyrim.Global.WICommentNextAllowed,
                                },
                            ],
                        },
                    ],
                }
            });
    }
}
