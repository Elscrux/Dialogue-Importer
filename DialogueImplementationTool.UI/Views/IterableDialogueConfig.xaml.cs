using System.Diagnostics;
using System.Windows;
using DialogueImplementationTool.UI.ViewModels;
namespace DialogueImplementationTool.UI.Views;

public partial class ProcessDialogue {
    private readonly IterableDialogueConfigVM _viewModel;

    public ProcessDialogue(IterableDialogueConfigVM iterableDialogueConfigVM) {
        InitializeComponent();
        _viewModel = iterableDialogueConfigVM;
        DataContext = _viewModel;

        iterableDialogueConfigVM.RefreshPreview(true);

        // Ensure ALL scene speakers are saved when the window closes
        Closing += (_, _) => {
            _viewModel.SaveAllSceneSpeakers();
        };
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        Close();
    }
}