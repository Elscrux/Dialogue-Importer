using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
using Quest = Mutagen.Bethesda.Skyrim.Quest;
namespace DialogueImplementationTool.Dialogue;

public sealed class WIRemoveItemTrashQuestFactory(IDialogueContext context, IVoiceTypeOrList voiceType) : IGenericDialogueQuestFactory {
    public string Name => context.Prefix + "WIRemoveItemDroppedTrash";
    public string ScriptName => Name + "Script";

    public Quest Create() {
        var voiceTypesList = (this as IGenericDialogueQuestFactory).GetVoiceTypesList(context);
        (this as IGenericDialogueQuestFactory).AddVoiceType(context, voiceType);

        context.Scripts.Add(ScriptName,
            $$"""
            Scriptname {{ScriptName}} extends WorldInteractionsScript Conditional
            {Extends WorldInteractionsScript which extends Quest script.}

            Event OnStoryRemoveFromPlayer(ObjectReference akOwner, ObjectReference akItem, Location akLocation, Form akItemBase, int aiRemoveType)
              SetNextEventGlobals()
            EndEvent
            """);

        var quest = context.GetOrAddRecord<Quest, IQuestGetter>(
            Name,
            () => {
                var questFormKey = context.GetNextFormKey();
                return new Quest(questFormKey, context.Release) {
                    EditorID = Name,
                    Name = "Just throw trash",
                    Priority = 30,
                    Filter = @"World Interactions\Remove Item\",
                    Event = RecordTypes.REMP,
                    DialogConditions = [],
                    VirtualMachineAdapter = new QuestAdapter {
                        Scripts = [
                            new ScriptEntry {
                                Name = ScriptName,
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
