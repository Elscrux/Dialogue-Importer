﻿using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
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

        this.WhenAnyValue(x => x.FormLink)
            .Subscribe(_ => speakerFavoritesSelection.AddSpeaker(new NpcSpeaker(linkCache, FormLink)));
    }

    public string Name { get; set; }
    [Reactive] public IFormLinkGetter FormLink { get; set; }
    public string? EditorID { get; set; }
}
