using System;
using System.Windows;
using DialogueImplementationTool.Parser;

namespace DialogueImplementationTool.UI;

public partial class MainWindow {
    public MainWindow() {
        InitializeComponent();
        DataContext = App.DialogueVM;
    }
    
    private void SelectFile_OnClick(object sender, RoutedEventArgs e) {
        App.DialogueVM.Clear();
        App.DialogueVM.DocumentParser = DocumentParser.LoadDocument() ?? DocumentParser.Null;
        if (App.DialogueVM.DocumentParser != DocumentParser.Null) new ProcessDialogue().ShowDialog();
    }
    
    private void SelectFolder_OnClick(object sender, RoutedEventArgs e) {
        throw new NotImplementedException();
    }
}