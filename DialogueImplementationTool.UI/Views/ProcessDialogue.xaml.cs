using System.Windows;
using System.Windows.Controls;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.UI.ViewModels;
namespace DialogueImplementationTool.UI.Views;

public partial class ProcessDialogue {
    private readonly DialogueVM _vm;

    public ProcessDialogue(DialogueVM vm) {
        _vm = vm;
        InitializeComponent();
        DataContext = _vm;

        _vm.RefreshPreview(true);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        _vm.Save.Execute(null);
        Close();
    }

    private void SpeakerFavouriteSelect_OnClick(object sender, RoutedEventArgs e) {
        var button = (Button) sender;
        var speaker = (NpcSpeaker) button.DataContext;
        _vm.SetSpeaker.Execute(speaker.FormKey);
    }
}
