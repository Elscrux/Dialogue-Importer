using System.Windows;
using DialogueImplementationTool.Dialogue.Speaker;
using GongSolutions.Wpf.DragDrop;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.WPF.Plugins;
namespace DialogueImplementationTool.UI.Services;

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
