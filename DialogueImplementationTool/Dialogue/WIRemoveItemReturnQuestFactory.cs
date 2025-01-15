using System.Linq;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
using Quest = Mutagen.Bethesda.Skyrim.Quest;
namespace DialogueImplementationTool.Dialogue;

public sealed class WIRemoveItemReturnQuestFactory(IDialogueContext context) : IGenericDialogueQuestFactory {
    private const string Name = "WIRemoveItemDroppedAccidentally";

    public FormList GetVoiceTypesList() {
        var voiceTypesListEditorId = context.Prefix + Name + "VoiceTypes";
        var voiceTypesList = context.LinkCache.PriorityOrder.WinningOverrides<IFormListGetter>()
            .FirstOrDefault(l => l.EditorID == voiceTypesListEditorId);

        FormList formList;
        if (voiceTypesList is null) {
            formList = new FormList(context.GetNextFormKey(), context.Release) {
                EditorID = voiceTypesListEditorId,
                Items = [],
            };
            context.AddRecord(formList);
        } else {
            formList = context.GetOrAddOverride<FormList, IFormListGetter>(voiceTypesList);
        }

        return formList;
    }

    public string GetQuestEditorId() {
        return context.Prefix + Name;
    }

    public string GetReturnItemScript() {
        return $"(GetOwningQuest() as {GetQuestScriptName()}).GiveItemBackToPlayer()";
    }

    public string GetQuestScriptName() {
        return GetQuestEditorId() + "Script";
    }

    public Condition GetIsBystanderAliasCondition() {
        return new GetIsAliasRefConditionData {
            ReferenceAliasIndex = 1,
        }.ToConditionFloat();
    }

    public Quest Create() {
        var questEditorId = GetQuestEditorId();

        var voiceTypesList = GetVoiceTypesList();

        var scriptName = GetQuestScriptName();
        context.Scripts.Add(scriptName,
            $$"""
            Scriptname {{scriptName}} extends WorldInteractionsScript Conditional
            {Extends WorldInteractionsScript which extends Quest script.}

            ReferenceAlias Property Item Auto

            Event OnStoryRemoveFromPlayer(ObjectReference akOwner, ObjectReference akItem, Location akLocation, Form akItemBase, int aiRemoveType)
              SetNextEventGlobals()
            EndEvent

            Function GiveItemBackToPlayer()
              Game.GetPlayer().AddItem(Item.GetRef())
              Stop()
            EndFunction
            """);

        return context.GetOrAddRecord<Quest, IQuestGetter>(
            questEditorId,
            () => {
                var questFormKey = context.GetNextFormKey();
                return new Quest(questFormKey, context.Release) {
                    EditorID = GetQuestEditorId(),
                    Name = "Accidentally dropped this",
                    Priority = 30,
                    Filter = @"World Interactions\Remove Item\",
                    Event = RecordTypes.REMP,
                    EventConditions = [
                        new GetKeywordDataForCurrentLocationConditionData {
                            Keyword = { Link = { FormKey = Skyrim.Keyword.WIComplexInteractionToggle.FormKey } },
                            Reference = Skyrim.PlayerRef,
                            RunOnType = Condition.RunOnType.Reference,
                        }.ToConditionFloat(compareOperator: CompareOperator.GreaterThanOrEqualTo, comparisonValue: 0),
                        new GetGlobalValueConditionData {
                            Global = { Link = { FormKey = Skyrim.Global.WIComplexEventsEnabled.FormKey } },
                        }.ToConditionFloat(),
                    ],
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
                                    new ScriptObjectProperty {
                                        Name = "Item",
                                        Flags = ScriptProperty.Flag.Edited,
                                        Object = new FormLink<ISkyrimMajorRecordGetter>(questFormKey),
                                        Alias = 0,
                                    },
                                ],
                            }
                        ],
                        Aliases = [
                            new QuestFragmentAlias {
                                Property = new ScriptObjectProperty {
                                    Object = new FormLink<ISkyrimMajorRecordGetter>(questFormKey),
                                    Alias = 0,
                                },
                                Scripts = [
                                    new ScriptEntry {
                                        Name = "WIRemoveItem02ItemScript",
                                        Flags = ScriptEntry.Flag.Local,
                                        Properties = [],
                                    }
                                ],
                            },
                            new QuestFragmentAlias {
                                Property = new ScriptObjectProperty {
                                    Object = new FormLink<ISkyrimMajorRecordGetter>(questFormKey),
                                    Alias = 2,
                                },
                                Scripts = [
                                    new ScriptEntry {
                                        Name = "WIPlayerScript",
                                        Flags = ScriptEntry.Flag.Local,
                                        Properties = [
                                            new ScriptBoolProperty {
                                                Name = "StopQuestOnLocationChange",
                                                Data = true,
                                                Flags = ScriptProperty.Flag.Edited,
                                            }
                                        ],
                                    }
                                ],
                            },
                        ]
                    },
                    NextAliasID = 3,
                    Aliases = [
                        new QuestAlias {
                            ID = 0,
                            Name = "Item",
                            FindMatchingRefFromEvent = new FindMatchingRefFromEvent {
                                FromEvent = RecordTypes.REMP,
                                EventData = QuestExtension.EventAlias1(),
                            },
                            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
                        },
                        new QuestAlias {
                            ID = 1,
                            Name = "Bystander",
                            FindMatchingRefFromEvent = new FindMatchingRefFromEvent {
                                FromEvent = RecordTypes.REMP,
                                EventData = QuestExtension.EventAlias1(),
                            },
                            Flags = QuestAlias.Flag.MatchingRefClosest | QuestAlias.Flag.MatchingRefInLoadedArea,
                            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
                            Conditions = [
                                new GetInFactionConditionData {
                                    Faction = {
                                        Link = { FormKey = HearthFires.Faction.BYOHRelationshipAdoptionFaction.FormKey }
                                    }
                                }.ToConditionFloat(comparisonValue: 0),
                                new GetInFactionConditionData {
                                    Faction = { Link = { FormKey = Skyrim.Faction.WINeverFillAliasesFaction.FormKey } }
                                }.ToConditionFloat(comparisonValue: 0),
                                new GetAllowWorldInteractionsConditionData().ToConditionFloat(comparisonValue: 1),
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
                            ],
                        },
                        new QuestAlias {
                            ID = 2,
                            Name = "Player",
                            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
                            ForcedReference = new FormLinkNullable<IPlacedGetter>(Skyrim.PlayerRef.FormKey),
                        },
                    ],
                };
            });
    }
}
