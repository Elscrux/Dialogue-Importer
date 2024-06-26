﻿using System.Collections.ObjectModel;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Services;

public interface ISpeakerFavoritesSelection {
    ReadOnlyObservableCollection<ISpeaker> Speakers { get; }
    void AddSpeaker(ISpeaker speaker);
    ISpeaker? GetSpeaker(FormKey formKey);
    ISpeaker? GetClosestSpeaker(string name);
}
