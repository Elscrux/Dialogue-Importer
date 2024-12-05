using System.Windows;
using DialogueImplementationTool.UI.ViewModels;
namespace DialogueImplementationTool.UI.Views;

public partial class ProcessDialogue {
    public ProcessDialogue(IterableDialogueConfigVM iterableDialogueConfigVM) {
        InitializeComponent();
        DataContext = iterableDialogueConfigVM;

        iterableDialogueConfigVM.RefreshPreview(true);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        // Saving is handled when registering that the window is closing
        Close();
    }
}
