using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
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
            .Subscribe(_ => speakerFavoritesSelection.AddSpeaker(new NpcSpeaker(linkCache, FormKey)));
    }

    public string Name { get; set; }
    [Reactive] public FormKey FormKey { get; set; }
    public string? EditorID { get; set; }
}
