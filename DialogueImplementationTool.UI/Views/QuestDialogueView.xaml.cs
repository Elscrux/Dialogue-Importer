using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using DialogueImplementationTool.UI.ViewModels;
using Xceed.Wpf.AvalonDock.Controls;
using Control = System.Windows.Controls.Control;
using UserControl = System.Windows.Controls.UserControl;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
namespace DialogueImplementationTool.UI.Views;

public partial class QuestDialogueView : UserControl {
    public QuestDialogueView() {
        InitializeComponent();
    }
    private static string GetFilter(QuestDialogueVM questDialogueVM) {
        var filterBuilder = new StringBuilder();
        var extensions = questDialogueVM.Extensions.ToList();
        for (var index = 0; index < questDialogueVM.Extensions.Count; index++) {
            filterBuilder.Append('*');
            filterBuilder.Append(extensions[index]);
            if (index != questDialogueVM.Extensions.Count - 1) filterBuilder.Append(';');
        }

        return filterBuilder.ToString();
    }

    private static string[] LoadDocument(QuestDialogueVM questDialogueVM) {
        var filter = GetFilter(questDialogueVM);
        var fileDialog = new OpenFileDialog {
            Multiselect = true,
            Filter = $"Documents({filter})|{filter}",
        };

        return fileDialog.ShowDialog() is true ? fileDialog.FileNames : [];
    }

    private static IEnumerable<string> LoadDocuments(QuestDialogueVM questDialogueVM) {
        var folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() != DialogResult.OK) yield break;

        foreach (var file in Directory.EnumerateFiles(
                folderDialog.SelectedPath,
                "*.*",
                SearchOption.AllDirectories)
            .Where(file => questDialogueVM.Extensions.Exists(file.EndsWith))) {
            yield return file;
        }
    }

    private void SelectFile_OnClick(object sender, RoutedEventArgs e) {
        if (sender is not Control { DataContext: QuestDialogueVM questDialogueVM }) return;

        var files = LoadDocument(questDialogueVM);

        foreach (var file in files) {
            questDialogueVM.AddDocument(file);
        }
    }

    private void SelectFolder_OnClick(object sender, RoutedEventArgs e) {
        if (sender is not Control { DataContext: QuestDialogueVM questDialogueVM }) return;

        var parsers = LoadDocuments(questDialogueVM);
        foreach (var parser in parsers) {
            questDialogueVM.AddDocument(parser);
        }
    }

    private void Delete(object sender, RoutedEventArgs e) {
        if (sender is not Control { DataContext: QuestDialogueVM questDialogueVM } control) return;

        var window = control.FindLogicalAncestor<Window>();
        if (window.DataContext is not MainWindowVM mainWindowVM) return;

        mainWindowVM.RemoveQuest(questDialogueVM.Quest.FormKey);
    }
}
