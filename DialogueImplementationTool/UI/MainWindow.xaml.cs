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
        var parser = DocumentParser.LoadDocument();
        if (parser == null) return;

        LaunchParser(parser);
    }
    
    private void SelectFolder_OnClick(object sender, RoutedEventArgs e) {
        var parsers = DocumentParser.LoadDocuments();
        foreach (var parser in parsers) LaunchParser(parser);
    }

    private void LaunchParser(DocumentParser parser) {
        if (parser == DocumentParser.Null) return;

        App.DialogueVM.Init(parser);
        new ProcessDialogue().ShowDialog();
    }
}