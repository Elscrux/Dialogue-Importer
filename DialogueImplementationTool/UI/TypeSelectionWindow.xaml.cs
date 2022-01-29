using System;
using System.Collections.ObjectModel;
using System.Windows;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Parser;

namespace DialogueImplementationTool.UI; 

public partial class TypeSelectionWindow {
    private readonly DocumentParser _documentParser;
    private readonly DialogueImplementer _dialogueImplementer = new();
    
    public ObservableCollection<string> DialogueTypes { get; set; } = new(Enum.GetNames(typeof(DialogueTypes)));
    
    public TypeSelectionWindow(DocumentParser documentParser) {
        _documentParser = documentParser;
        InitializeComponent();
        
        Refresh();
    }

    private void Implement_OnClick(object sender, RoutedEventArgs e) {
        var dialogue = _documentParser.ParseNext();
        if (dialogue.Count == 0) return;

        _dialogueImplementer.AddDialogue(dialogue);
        Refresh();
    }
    
    private void Greeting_OnClick(object sender, RoutedEventArgs e) {
        var dialogue = _documentParser.ParseNext();
        if (dialogue.Count == 0) return;

        _dialogueImplementer.AddGreeting(dialogue);
        Refresh();
    }
    
    private void Farewell_OnClick(object sender, RoutedEventArgs e) {
        var dialogue = _documentParser.ParseNext();
        if (dialogue.Count == 0) return;

        _dialogueImplementer.AddFarewell(dialogue);
        Refresh();
    }
    
    private void Dialogue_OnClick(object sender, RoutedEventArgs e) {
        var dialogue = _documentParser.ParseNext();
        if (dialogue.Count == 0) return;

        _dialogueImplementer.AddDialogue(dialogue);
        Refresh();
    }
    
    private void Idle_OnClick(object sender, RoutedEventArgs e) {
        var dialogue = _documentParser.ParseNext();
        if (dialogue.Count == 0) return;

        _dialogueImplementer.AddIdle(dialogue);
        Refresh();
    }
    
    private void GenericScene_OnClick(object sender, RoutedEventArgs e) {
        var dialogue = _documentParser.ParseNext();
        if (dialogue.Count == 0) return;

        _dialogueImplementer.AddScene(dialogue);
        Refresh();
    }
    
    private void QuestScene_OnClick(object sender, RoutedEventArgs e) {
        var dialogue = _documentParser.ParseNext();
        if (dialogue.Count == 0) return;

        _dialogueImplementer.AddQuestScene(dialogue);
        Refresh();
    }
    
    private void SkipOne_OnClick(object sender, RoutedEventArgs e) {
        _documentParser.SkipOne();
        Refresh();
    }
    
    private void SkipMany_OnClick(object sender, RoutedEventArgs e) {
        _documentParser.SkipMany();
        Refresh();

    }

    private void Refresh() {
        var preview = string.Empty;
        while (string.IsNullOrWhiteSpace(preview)) {
            if (_documentParser.HasFinished()) {
                _dialogueImplementer.Save();
                Close();
                return;
            }

            preview = _documentParser.PreviewCurrent();
            if (string.IsNullOrEmpty(preview)) {
                _documentParser.SkipOne();
            } else {
                PreviewText.Text = preview;
            }
        }
    }
}