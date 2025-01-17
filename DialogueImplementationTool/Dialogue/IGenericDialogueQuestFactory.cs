using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
namespace DialogueImplementationTool.Dialogue;

public interface IGenericDialogueQuestFactory {
    Quest Create();

    string Name { get; }

    void AddVoiceType(IDialogueContext context, IVoiceTypeOrList voiceTypeOrList) {
        var voiceTypesList =  GetVoiceTypesList(context);
        
        switch (voiceTypeOrList) {
            case IFormList formList: {
                foreach (var item in formList.Items) {
                    if (context.LinkCache.TryResolve<IVoiceTypeGetter>(item.FormKey, out var voiceType)) {
                        TryAddVoiceType(voiceType);
                    }
                }
                break;
            }
            case IVoiceType voiceType: {
                TryAddVoiceType(voiceType);
                break;
            }
            default: throw new ArgumentOutOfRangeException(nameof(voiceTypeOrList));
        }

        void TryAddVoiceType(IVoiceTypeGetter voiceType) {
            if (voiceTypesList.Items.All(i => i.FormKey != voiceType.FormKey)) {
                voiceTypesList.Items.Add(new FormLink<ISkyrimMajorRecordGetter>(voiceType));
            }
        }
    }

    FormList GetVoiceTypesList(IDialogueContext context) {
        var voiceTypesListEditorId = Name + "VoiceTypes";
        return context.GetOrAddRecord<FormList, IFormListGetter>(voiceTypesListEditorId,
            () => new FormList(context.GetNextFormKey(), context.Release) {
                EditorID = voiceTypesListEditorId,
                Items = [],
            });
    }
}
