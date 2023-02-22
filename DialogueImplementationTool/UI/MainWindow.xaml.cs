using System.IO;
using System.Linq;
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

        //Save warning
        if (!App.DialogueVM.SavedSession && App.DialogueVM.DialogueTypeList.All(selection => selection.Selection.Any(type => type.Value))) {
            if (MessageBox.Show("You didn't save your changes, do you want to save now?", string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                App.DialogueVM.Save.Execute(null);
            }
        }
    }
}