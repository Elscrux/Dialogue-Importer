using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public interface IGenericDialogueQuestFactory {
    Quest Create();

    string Name { get; }

    void AddVoiceType(IDialogueContext context, VoiceType voiceType) {
        var voiceTypesList =  GetVoiceTypesList(context);
        if (voiceTypesList.Items.All(i => i.FormKey != voiceType.FormKey)) {
            voiceTypesList.Items.Add(new FormLink<ISkyrimMajorRecordGetter>(voiceType));
        }
    }

    FormList GetVoiceTypesList(IDialogueContext context) {
        var voiceTypesListEditorId = context.Prefix + Name + "VoiceTypes";
        return context.GetOrAddRecord<FormList, IFormListGetter>(voiceTypesListEditorId,
            () => new FormList(context.GetNextFormKey(), context.Release) {
                EditorID = voiceTypesListEditorId,
                Items = [],
            });
    }
}
