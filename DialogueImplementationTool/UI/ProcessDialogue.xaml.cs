using System.Windows;
using DialogueImplementationTool.Dialogue;

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

    private void Finish_OnClick(object sender, RoutedEventArgs e) {
        App.DialogueVM.DialogueImplementer.ImplementDialogue(App.DialogueVM.DocumentParser.GetDialogue());
        DialogueFactory.Save();
    }
}