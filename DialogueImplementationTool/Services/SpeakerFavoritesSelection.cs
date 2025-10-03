using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using ReactiveUI;
namespace DialogueImplementationTool.Services;

public sealed class SpeakerFavoritesSelection : ReactiveObject, ISpeakerFavoritesSelection {
    private readonly IPrefixProvider _prefixProvider;
    private readonly ObservableCollection<ISpeaker> _speakers = [];

    public SpeakerFavoritesSelection(IPrefixProvider prefixProvider) {
        _prefixProvider = prefixProvider;
        Speakers = new ReadOnlyObservableCollection<ISpeaker>(_speakers);
    }

    public ReadOnlyObservableCollection<ISpeaker> Speakers { get; }

    public void AddSpeaker(ISpeaker speaker) {
        if (speaker.FormLink.IsNull) return;
        if (Speakers.Any(s => s.FormLink.FormKey == speaker.FormLink.FormKey)) return;

        _speakers.Add(speaker);
    }

    public ISpeaker? GetSpeaker(FormKey formKey) {
        return Speakers.FirstOrDefault(s => s.FormLink.FormKey == formKey);
    }

    public IEnumerable<ISpeaker> GetClosestSpeakers(string name) {
        return Speakers
            .Select(s => (Speakers: s, Index: s.EditorID?.IndexOf(name, StringComparison.Ordinal)))
            .Where(x => x.Index is not null and not -1)
            .OrderBy(x => {
                // Reward matches that start with the prefix
                if (x.Speakers.EditorID?.StartsWith(_prefixProvider.Prefix) is true) {
                    x.Index -= _prefixProvider.Prefix.Length;
                }

                return x.Index;
            })
            .Select(x => x.Speakers);
    }
}
