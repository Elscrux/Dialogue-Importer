using System.Collections.ObjectModel;
using System.Windows;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Models;
using GongSolutions.Wpf.DragDrop;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.WPF.Plugins;
using Noggog;
namespace DialogueImplementationTool.UI.Views;

public partial class SceneSpeakerWindow {
    public SceneSpeakerWindow(
        ILinkCache linkCache,
        ISpeakerFavoritesSelection speakerFavoritesSelection,
        ObservableCollection<AliasSpeakerSelection> speakers) {
        InitializeComponent();

        LinkCache = linkCache;
        SpeakerFavoritesSelection = speakerFavoritesSelection;
        ScopedTypes = typeof(INpcGetter).AsEnumerable();
        SceneSpeakers = speakers;

        // Assign the closest existing favorites
        foreach (var speaker in SceneSpeakers) {
            var matchingSpeaker = SpeakerFavoritesSelection.GetClosestSpeakers(speaker.Name).FirstOrDefault();
            if (matchingSpeaker is not null) speaker.FormKey = matchingSpeaker.FormLink.FormKey;
        }

        DataContext = this;
    }

    public ObservableCollection<AliasSpeakerSelection> SceneSpeakers { get; }
    public ILinkCache LinkCache { get; }
    public ISpeakerFavoritesSelection SpeakerFavoritesSelection { get; }
    public IEnumerable<Type> ScopedTypes { get; set; }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        Close();
    }
}

public sealed class FormKeyWrapper {
    public FormKey FormKey { get; set; }
}

public sealed class GridFormKeyPickerDropTarget : IDropTarget {
    public void DragOver(IDropInfo dropInfo) {
        if (dropInfo.Data is FormKeyWrapper) {
            dropInfo.Effects = DragDropEffects.Copy;
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        }
    }

    public void Drop(IDropInfo dropInfo) {
        if (dropInfo.Data is FormKeyWrapper wrapper && dropInfo.VisualTarget is FormKeyPicker formKeyPicker)
            formKeyPicker.FormKey = wrapper.FormKey;
    }
}

public sealed class SpeakerFavouriteFormKeyDragSource : IDragSource {
    public void StartDrag(IDragInfo dragInfo) {
        if (dragInfo.SourceItem is NpcSpeaker speakerFavourite) {
            dragInfo.Data = new FormKeyWrapper { FormKey = speakerFavourite.FormLink.FormKey };
            dragInfo.Effects = DragDropEffects.Copy;
        }
    }

    public bool CanStartDrag(IDragInfo dragInfo) {
        return true;
    }

    public void Dropped(IDropInfo dropInfo) {}
    public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo) {}
    public void DragCancelled() {}

    public bool TryCatchOccurredException(Exception exception) {
        return true;
    }
}
