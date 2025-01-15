using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
using Quest = Mutagen.Bethesda.Skyrim.Quest;
namespace DialogueImplementationTool.Dialogue;

public sealed class WIRemoveItemTrashQuestFactory(IDialogueContext context) : IGenericDialogueQuestFactory {
    private const string Name = "WIRemoveItemDroppedTrash";

    public FormList GetVoiceTypesList() {
        var voiceTypesListEditorId = context.Prefix + Name + "VoiceTypes";
        return context.GetOrAddRecord<FormList, IFormListGetter>(voiceTypesListEditorId,
            () => new FormList(context.GetNextFormKey(), context.Release) {
                EditorID = voiceTypesListEditorId,
                Items = [],
            });
    }

    public string GetQuestEditorId() {
        return context.Prefix + Name;
    }

    public string GetQuestScriptName() {
        return GetQuestEditorId() + "Script";
    }

    public Quest Create() {
        var questEditorId = GetQuestEditorId();

        var voiceTypesList = GetVoiceTypesList();

        var scriptName = GetQuestScriptName();
        context.Scripts.Add(scriptName,
            $$"""
            Scriptname {{scriptName}} extends WorldInteractionsScript Conditional
            {Extends WorldInteractionsScript which extends Quest script.}

            Event OnStoryRemoveFromPlayer(ObjectReference akOwner, ObjectReference akItem, Location akLocation, Form akItemBase, int aiRemoveType)
              SetNextEventGlobals()
            EndEvent
            """);

        var quest = context.GetOrAddRecord<Quest, IQuestGetter>(
            questEditorId,
            () => {
                var questFormKey = context.GetNextFormKey();
                return new Quest(questFormKey, context.Release) {
                    EditorID = GetQuestEditorId(),
                    Name = "Just throw trash",
                    Priority = 30,
                    Filter = @"World Interactions\Remove Item\",
                    Event = RecordTypes.REMP,
                    DialogConditions = [],
                    VirtualMachineAdapter = new QuestAdapter {
                        Scripts = [
                            new ScriptEntry {
                                Name = scriptName,
                                Flags = ScriptEntry.Flag.Local,
                                Properties = [
                                    new ScriptObjectProperty {
                                        Name = "pGameDaysPassed",
                                        Flags = ScriptProperty.Flag.Edited,
                                        Object = Skyrim.Global.GameDaysPassed,
                                    },
                                    new ScriptObjectProperty {
                                        Name = "pWINextEvent",
                                        Flags = ScriptProperty.Flag.Edited,
                                        Object = Skyrim.Global.WINextRemoveItem,
                                    },
                                    new ScriptObjectProperty {
                                        Name = "pWIWaitEvent",
                                        Flags = ScriptProperty.Flag.Edited,
                                        Object = Skyrim.Global.WIWaitRemoveItem,
                                    },
                                ],
                            }
                        ],
                    },
                    NextAliasID = 1,
                    Aliases = [
                        new QuestAlias {
                            ID = 0,
                            Name = "Spectator",
                            FindMatchingRefFromEvent = new FindMatchingRefFromEvent(),
                            Flags = QuestAlias.Flag.MatchingRefClosest | QuestAlias.Flag.MatchingRefInLoadedArea,
                            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
                            Conditions = [
                                new GetInFactionConditionData {
                                    Faction = { Link = { FormKey = Skyrim.Faction.WINeverFillAliasesFaction.FormKey } }
                                }.ToConditionFloat(comparisonValue: 0),
                                new GetAllowWorldInteractionsConditionData().ToConditionFloat(),
                                new GetIsVoiceTypeConditionData {
                                    VoiceTypeOrList = { Link = { FormKey = voiceTypesList.FormKey } }
                                }.ToConditionFloat(comparisonValue: 0),
                                new HasKeywordConditionData {
                                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                                }.ToConditionFloat(),
                                new ConditionGlobal {
                                    Data = new GetDistanceConditionData {
                                        Target = { Link = { FormKey = Skyrim.PlayerRef.FormKey } },
                                    },
                                    CompareOperator = CompareOperator.LessThanOrEqualTo,
                                    ComparisonValue = Skyrim.Global.WISpectatorDistance
                                },
                                new GetDetectedConditionData {
                                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } },
                                }.ToConditionFloat(),
                            ],
                        },
                    ],
                };
            });

        return quest;
    }
}
