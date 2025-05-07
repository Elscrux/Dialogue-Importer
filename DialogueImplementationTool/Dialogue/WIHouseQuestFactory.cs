using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
using LocationHasKeywordConditionData = Mutagen.Bethesda.Skyrim.LocationHasKeywordConditionData;
namespace DialogueImplementationTool.Dialogue;

public sealed class WIHouseQuestFactory(IDialogueContext context, IVoiceTypeOrList voiceType) : IGenericDialogueQuestFactory {
    public string Name => context.Prefix + "WIHouse";

    public Quest Create() {
        var voiceTypesList = (this as IGenericDialogueQuestFactory).GetVoiceTypesList(context);
        (this as IGenericDialogueQuestFactory).AddVoiceType(context, voiceType);

        var quest = context.GetOrAddRecord<Quest, IQuestGetter>(
            Name,
            () => {
                var questFormKey = context.GetNextFormKey();
                return new Quest(questFormKey, context.Release) {
                    EditorID = Name,
                    Name = "House Interactions",
                    Priority = 40,
                    Filter = @"World Interactions\House\",
                    Event = RecordTypes.CLOC,
                    EventConditions = [
                        new GetEventDataConditionData {
                            Function = GetEventDataConditionData.EventFunction.HasKeyword,
                            Member = GetEventDataConditionData.EventMember.NewLocation,
                            Record = new FormLink<ISkyrimMajorRecordGetter>(Skyrim.Keyword.LocTypeHouse.FormKey),
                        }.ToConditionFloat(),
                    ],
                    NextAliasID = 2,
                    Aliases = [
                        new QuestAlias {
                            ID = 0,
                            Type = QuestAlias.TypeEnum.Location,
                            Name = "Location",
                            FindMatchingRefFromEvent = new FindMatchingRefFromEvent {
                                FromEvent = RecordTypes.CLOC,
                                EventData = (byte[]) [0x4C, 0x32, 0x0, 0x0],
                            },
                            Flags = QuestAlias.Flag.AllowReserved,
                            Conditions = [
                                new LocationHasKeywordConditionData {
                                    Keyword = { Link = { FormKey = Skyrim.Keyword.LocTypeStore.FormKey } },
                                }.ToConditionFloat(),
                            ],
                        },
                        new QuestAlias {
                            ID = 1,
                            Name = "HomeOwner",
                            FindMatchingRefFromEvent = new FindMatchingRefFromEvent(),
                            Flags = QuestAlias.Flag.MatchingRefClosest | QuestAlias.Flag.MatchingRefInLoadedArea | QuestAlias.Flag.AllowReserved,
                            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
                            Conditions = [
                                new GetIsEditorLocAliasConditionData { LocationAliasIndex = 0 }.ToConditionFloat(),
                                new GetInCurrentLocAliasConditionData { LocationAliasIndex = 0 }.ToConditionFloat(),
                                new GetAllowWorldInteractionsConditionData().ToConditionFloat(),
                                new GetInFactionConditionData {
                                    Faction = { Link = { FormKey = Skyrim.Faction.WINoGreetingFaction.FormKey } }
                                }.ToConditionFloat(comparisonValue: 0),
                                new GetInFactionConditionData {
                                    Faction = { Link = { FormKey = Skyrim.Faction.WINeverFillAliasesFaction.FormKey } }
                                }.ToConditionFloat(comparisonValue: 0),
                                new GetSleepingConditionData().ToConditionFloat(comparisonValue: 0),
                                new GetDetectedConditionData {
                                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } },
                                }.ToConditionFloat(),
                                new GetIsVoiceTypeConditionData {
                                    VoiceTypeOrList = { Link = { FormKey = voiceTypesList.FormKey } }
                                }.ToConditionFloat(comparisonValue: 0),
                            ],
                        },
                    ],
                };
            });

        return quest;
    }
}
