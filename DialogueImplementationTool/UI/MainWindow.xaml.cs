using System.IO;
using System.Windows;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;

namespace DialogueImplementationTool.UI;

public partial class MainWindow {
    public MainWindow() {
        var pathProvider = new PluginListingsPathProvider(new GameReleaseInjection(GameRelease.SkyrimSE));
        if (!File.Exists(pathProvider.Path)) MessageBox.Show($"Make sure {pathProvider.Path} exists.");

        InitializeComponent();
        DataContext = App.DialogueVM;
    }
    
    private void SelectFile_OnClick(object sender, RoutedEventArgs e) {
        App.DialogueVM.Clear();
        App.DialogueVM.DocumentParser = DocumentParser.LoadDocument() ?? DocumentParser.Null;
        if (App.DialogueVM.DocumentParser != DocumentParser.Null) new ProcessDialogue().ShowDialog();
    }
    
    private void SelectFolder_OnClick(object sender, RoutedEventArgs e) {
        App.DialogueVM.Clear();

        var parsers = DocumentParser.LoadDocuments();
        foreach (var parser in parsers) {
            App.DialogueVM.DocumentParser = parser;
            if (App.DialogueVM.DocumentParser != DocumentParser.Null) new ProcessDialogue().ShowDialog();
        }
    }
}