using System.Windows;
using DialogueImplementationTool.UI.ViewModels;
namespace DialogueImplementationTool.UI.Views;

public partial class ProcessDialogue {
    public ProcessDialogue(IterableDialogueConfigVM iterableDialogueConfigVM) {
        InitializeComponent();
        DataContext = iterableDialogueConfigVM;

        iterableDialogueConfigVM.RefreshPreview(true);

        // Ensure ALL scene speakers are saved when the window closes
        Closing += (_, _) => {
            iterableDialogueConfigVM.SaveAllSceneSpeakers();
        };
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        Close();
    }
}
