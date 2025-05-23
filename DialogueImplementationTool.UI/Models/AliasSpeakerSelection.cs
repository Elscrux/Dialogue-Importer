using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.Models;

public sealed class AliasSpeakerSelection : ReactiveObject {
    public AliasSpeakerSelection(
        ILinkCache linkCache,
        ISpeakerFavoritesSelection speakerFavoritesSelection,
        string name) {
        Name = ISpeaker.GetSpeakerName(name);

        this.WhenAnyValue(x => x.FormKey)
            .Subscribe(formKey => {
                var formLink = new FormLinkInformation(formKey, typeof(INpcGetter));
                speakerFavoritesSelection.AddSpeaker(new NpcSpeaker(linkCache, formLink));
            });
    }

    public string Name { get; set; }
    public IFormLinkGetter FormLink => new FormLinkInformation(FormKey, typeof(INpcGetter));
    [Reactive] public FormKey FormKey { get; set; } = FormKey.Null;
    public string? EditorID { get; set; }
}
