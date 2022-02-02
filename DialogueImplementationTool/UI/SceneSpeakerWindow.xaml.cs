using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.UI; 

public partial class SceneSpeakerWindow {
    public ObservableCollection<Speaker> SceneSpeakers { get; }

    public ILinkCache LinkCache { get; }
    public IEnumerable<Type> ScopedTypes { get; set; }

    public SceneSpeakerWindow(ObservableCollection<Speaker> speakers) {
        InitializeComponent();
        
        SceneSpeakers = speakers;
        
        LinkCache = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE, LinkCachePreferences.OnlyIdentifiers()).LinkCache;
        ScopedTypes = typeof(INpcGetter).AsEnumerable();

        DataContext = this;
    }
}

public class Speaker {
    public Speaker(string name) => Name = name;

    public string Name { get; }
    public FormKey FormKey { get; set; }
    public string? EditorID { get; set; }
    public int AliasIndex { get; set; } = -1;
}