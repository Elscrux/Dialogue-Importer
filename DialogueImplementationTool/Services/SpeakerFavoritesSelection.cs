﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using ReactiveUI;
namespace DialogueImplementationTool.Services;

public sealed class SpeakerFavoritesSelection : ReactiveObject, ISpeakerFavoritesSelection {
    private readonly ObservableCollection<ISpeaker> _speakers = [];

    public SpeakerFavoritesSelection() {
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

    public ISpeaker? GetClosestSpeaker(string name) {
        var closestSpeaker = Speakers.MinBy(
            s => {
                var index = s.EditorID?.IndexOf(name, StringComparison.Ordinal);
                return index is null or -1 ? int.MaxValue : index;
            });

        // Check if the closest speaker is a match
        if (closestSpeaker?.EditorID?.IndexOf(name, StringComparison.Ordinal) is null or -1) return null;

        return closestSpeaker;
    }
}
