using System.Windows;
using DialogueImplementationTool.UI.ViewModels;
namespace DialogueImplementationTool.UI.Views;

public partial class ProcessDialogue {
    public ProcessDialogue(DialogueVM vm) {
        InitializeComponent();
        DataContext = vm;

        vm.RefreshPreview(true);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        // Saving is handled when registering that the window is closing
        Close();
    }
}
