using System.Windows;
using System.Windows.Controls;
using DialogueImplementationTool.Dialogue;
namespace DialogueImplementationTool.UI;

public partial class ProcessDialogue {
    public ProcessDialogue() {
        InitializeComponent();
        DataContext = App.DialogueVM;

        App.DialogueVM.RefreshPreview(true);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) => App.DialogueVM.Save.Execute(null);

    private void OpenFolder_OnClick(object sender, RoutedEventArgs e) => App.DialogueVM.OpenOutput();
    
    private void SpeakerFavouriteSelect_OnClick(object sender, RoutedEventArgs e) {
        var button = (Button) sender;
        var speaker = (Speaker) button.DataContext;
        App.DialogueVM.SetSpeaker.Execute(speaker.FormKey);
    }
}