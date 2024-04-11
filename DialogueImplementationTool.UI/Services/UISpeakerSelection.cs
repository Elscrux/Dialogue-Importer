using System.Collections.ObjectModel;
using System.Windows;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.UI.Models;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.UI.Services;

public sealed class UISpeakerSelection(ILinkCache linkCache, ISpeakerFavoritesSelection speakerFavoritesSelection)
    : ISpeakerSelection {
    public IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IEnumerable<string> speakerNames) {
        var speakers = new ObservableCollection<AliasSpeakerSelection>(speakerNames
            .Select(s => new AliasSpeakerSelection(linkCache, speakerFavoritesSelection, s))
            .ToList());
        new SceneSpeakerWindow(linkCache, speakerFavoritesSelection, speakers).ShowDialog();

        while (speakers.Any(s => s.FormKey == FormKey.Null)) {
            MessageBox.Show("You must assign every speaker of the scene to an npc");
            new SceneSpeakerWindow(linkCache, speakerFavoritesSelection, speakers).ShowDialog();
        }

        return speakers
            .Select(x => new AliasSpeaker(x.FormKey, x.Name, editorId: x.EditorID))
            .ToList();
    }
}
