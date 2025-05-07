using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class VoiceTypeGenericDialogueQuestFactory(IDialogueContext context, IVoiceTypeOrList voiceTypeOrList) : IGenericDialogueQuestFactory {
    public string Name => context.Prefix + "GenericDialogue" + voiceTypeOrList.EditorID?.TrimStart(context.Prefix);

    public Quest Create() {
        return context.GetOrAddRecord<Quest, IQuestGetter>(
            Name,
            () => new Quest(context.GetNextFormKey(), context.Release) {
                EditorID = Name,
                Priority = 0,
                Filter = context.Quest.Filter,
                DialogConditions = [
                    new IsCommandedActorConditionData().ToConditionFloat(comparisonValue: 0, or: true),
                    new HasKeywordConditionData {
                        Keyword = { Link = { FormKey = Update.Keyword.CommandedVoiceExcluded.FormKey } }
                    }.ToConditionFloat(),
                    new GetIsVoiceTypeConditionData {
                        VoiceTypeOrList = { Link = { FormKey = voiceTypeOrList.FormKey } },
                    }.ToConditionFloat(),
                ],
                Name = $"Generic Dialogue for {voiceTypeOrList.EditorID}",
                Flags = Quest.Flag.StartGameEnabled,
            });
    }
}
