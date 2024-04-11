using System.Collections.ObjectModel;
using DialogueImplementationTool.Dialogue;
using Mutagen.Bethesda.Plugins;
using ReactiveUI;
namespace DialogueImplementationTool.UI.Models;

public sealed class SpeakerFavoritesSelection : ReactiveObject, ISpeakerFavoritesSelection {
    private readonly ObservableCollection<ISpeaker> _speakers = [];

    public SpeakerFavoritesSelection() {
        Speakers = new ReadOnlyObservableCollection<ISpeaker>(_speakers);
    }

    public ReadOnlyObservableCollection<ISpeaker> Speakers { get; }

    public void AddSpeaker(ISpeaker speaker) {
        if (speaker.FormKey.IsNull) return;
        if (Speakers.Any(s => s.FormKey == speaker.FormKey)) return;

        _speakers.Add(speaker);
    }

    public ISpeaker? GetSpeaker(FormKey formKey) {
        return Speakers.FirstOrDefault(s => s.FormKey == formKey);
    }

    public ISpeaker? GetClosestSpeaker(string name) {
        return Speakers.MinBy(
            s => {
                var index = s.EditorID?.IndexOf(name, StringComparison.OrdinalIgnoreCase);
                return index is null or -1 ? int.MaxValue : index;
            });
    }
}
