using System.Windows;
using System.Windows.Controls;
using DialogueImplementationTool.Parser;

namespace DialogueImplementationTool.UI;

public partial class ProcessDialogue {
    public ProcessDialogue() {
        InitializeComponent();
        DataContext = App.DialogueVM;
        
        RefreshPreview(true);
    }
    
    private void BacktrackMany_OnClick(object sender, RoutedEventArgs e) {
        App.DialogueVM.DocumentParser.BacktrackMany();
        RefreshPreview(false);
    }
    
    private void Previous_OnClick(object sender, RoutedEventArgs e) {
        App.DialogueVM.DocumentParser.Previous();
        RefreshPreview(false);
    }
    
    private void Next_OnClick(object sender, RoutedEventArgs e) {
        App.DialogueVM.DocumentParser.Next();
        RefreshPreview(true);
    }

    private void SkipMany_OnClick(object sender, RoutedEventArgs e) {
        App.DialogueVM.DocumentParser.SkipMany();
        RefreshPreview(true);
    }

    private void RefreshPreview(bool forward) {
        var preview = string.Empty;
        var tries = 0;
        while (string.IsNullOrWhiteSpace(preview) && tries < 10) {
            preview = App.DialogueVM.DocumentParser.PreviewCurrent();
            if (string.IsNullOrEmpty(preview)) {
                if (forward) {
                    App.DialogueVM.DocumentParser.Next();   
                } else {
                    App.DialogueVM.DocumentParser.Previous();
                }
            } else {
                PreviewText.Text = preview;
            }
            tries++;
        }
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) => App.DialogueVM.Save();

    private void OpenFolder_OnClick(object sender, RoutedEventArgs e) => App.DialogueVM.OpenOutput();
    
    private void SpeakerFavouriteSelect_OnClick(object sender, RoutedEventArgs e) {
        var button = (Button) sender;
        var speaker = (SpeakerFavourite) button.DataContext;
        App.DialogueVM.SetSpeaker.Execute(speaker.FormKey);
    }
}