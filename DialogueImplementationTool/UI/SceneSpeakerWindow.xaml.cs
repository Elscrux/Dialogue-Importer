using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DialogueImplementationTool.Parser;
using GongSolutions.Wpf.DragDrop;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.WPF.Plugins;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI; 

public partial class SceneSpeakerWindow {
    public ObservableCollection<Speaker> SceneSpeakers { get; }
    public ObservableCollection<SpeakerFavourite> SpeakerFavourites => App.DialogueVM.SpeakerFavourites;

    public ILinkCache LinkCache { get; }
    public IEnumerable<Type> ScopedTypes { get; set; }

    public SceneSpeakerWindow(ObservableCollection<Speaker> speakers) {
        InitializeComponent();
        
        SceneSpeakers = speakers;
        
        foreach (var speaker in SceneSpeakers) {
            var matchingSpeaker = SpeakerFavourites.FirstOrDefault(s => s.EditorID != null && s.EditorID.ToLower().Contains(speaker.Name.ToLower()));
            if (matchingSpeaker != null) speaker.FormKey = matchingSpeaker.FormKey;
        }
        
        LinkCache = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE, LinkCachePreferences.OnlyIdentifiers()).LinkCache;
        ScopedTypes = typeof(INpcGetter).AsEnumerable();

        DataContext = this;
    }
}

public class FormKeyWrapper {
    public FormKey FormKey { get; set; }
}

public class GridFormKeyPickerDropTarget : IDropTarget {
    public void DragOver(IDropInfo dropInfo) {
        if (dropInfo.Data is FormKeyWrapper) {
            dropInfo.Effects = DragDropEffects.Copy;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        }
    }
    
    public void Drop(IDropInfo dropInfo) {
        if (dropInfo.Data is FormKeyWrapper wrapper && dropInfo.VisualTarget is FormKeyPicker formKeyPicker) {
            formKeyPicker.FormKey = wrapper.FormKey;
        }
    }
} 

public class SpeakerFavouriteFormKeyDragSource : IDragSource {
    public void StartDrag(IDragInfo dragInfo) {
        if (dragInfo.SourceItem is SpeakerFavourite speakerFavourite) {
            dragInfo.Data = new FormKeyWrapper { FormKey = speakerFavourite.FormKey };
            dragInfo.Effects = DragDropEffects.Copy;
        }
    }
    public bool CanStartDrag(IDragInfo dragInfo) => true;
    public void Dropped(IDropInfo dropInfo) {}
    public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo) {}
    public void DragCancelled() {}
    public bool TryCatchOccurredException(Exception exception) { return true; }
} 

public class Speaker : ReactiveObject {
    public Speaker(string name) {
        Name = name;

        this.WhenAnyValue(x => x.FormKey)
            .Subscribe(_ => { 
                if (FormKey != FormKey.Null && App.DialogueVM.SpeakerFavourites.All(s => s.FormKey != FormKey)) {
                    var record = App.DialogueVM.LinkCache.Resolve<INpcGetter>(FormKey);
                    App.DialogueVM.SpeakerFavourites.Add(new SpeakerFavourite(FormKey, record.EditorID));
                }
            });
    }

    public string Name { get; }
    [Reactive] public FormKey FormKey { get; set; }
    public string? EditorID { get; set; }
    public int AliasIndex { get; set; } = -1;
}