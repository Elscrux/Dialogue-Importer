using System.Collections.Generic;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
namespace DialogueImplementationTool.Dialogue;

/// <summary>
/// Used for custom WICastMagic reactions.
/// The problem is that WICastMagic lines are hardcoded to always play from specific vanilla dialog topics.
/// When multiple Beyond Skyrim projects add topics to this, they will conflict.
/// </summary>
public sealed class CustomWICastMagicReactionFactory(
    IDialogueContext context,
    string reactionType,
    string description,
    FormKey reactionEventFormKey,
    IEnumerable<Condition> extraConditions)
    : IGenericDialogueQuestFactory {
    public string Name => $"{context.Prefix}WICastMagic{reactionType}Reaction";

    public Quest Create() {
        return context.GetOrAddRecord<Quest, IQuestGetter>(
            Name,
            () => new Quest(context.GetNextFormKey(), context.Release) {
                EditorID = Name,
                Name = description,
                Priority = 0,
                Filter = @"World Interactions\Comment\",
                Event = RecordTypes.SCPT,
                DialogConditions = [..extraConditions,],
                EventConditions = [
                    new GetEventDataConditionData {
                        Function = GetEventDataConditionData.EventFunction.GetIsID,
                        Member = GetEventDataConditionData.EventMember.Keyword,
                        Record = new FormLink<ISkyrimMajorRecordGetter>(reactionEventFormKey),
                        FirstUnusedStringParameter = null,
                        SecondUnusedStringParameter = null
                    }.ToConditionFloat()
                ],
                Aliases = [
                    new QuestAlias {
                        ID = 0,
                        Name = "Speaker",
                        Type = QuestAlias.TypeEnum.Reference,
                        FindMatchingRefFromEvent = new FindMatchingRefFromEvent {
                            FromEvent = RecordTypes.SCPT,
                            EventData = (byte[]) [0x52, 0x31, 0x0, 0x0],
                        },
                    },
                ],
            });
    }
}
