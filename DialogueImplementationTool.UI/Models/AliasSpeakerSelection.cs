using System.Reactive.Linq;
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

        this.WhenAnyValue(x => x.FormLink)
            .Where(link => !link.IsNull)
            .Subscribe(link => speakerFavoritesSelection.AddSpeaker(new NpcSpeaker(linkCache, link)));
    }

    public string Name { get; set; }
    [Reactive] public IFormLinkGetter FormLink { get; set; } = new FormLinkInformation(FormKey.Null, typeof(INpcGetter));
    public string? EditorID { get; set; }
}
