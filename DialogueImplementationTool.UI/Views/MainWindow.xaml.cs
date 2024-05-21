using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using DialogueImplementationTool.UI.ViewModels;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
namespace DialogueImplementationTool.UI.Views;

public partial class MainWindow {
    private readonly MainWindowVM _vm;

    public MainWindow(MainWindowVM vm) {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    private string GetFilter() {
        var filterBuilder = new StringBuilder();
        var extensions = _vm.Extensions.ToList();
        for (var index = 0; index < _vm.Extensions.Count; index++) {
            filterBuilder.Append('*');
            filterBuilder.Append(extensions[index]);
            if (index != _vm.Extensions.Count - 1) filterBuilder.Append(';');
        }

        return filterBuilder.ToString();
    }

    private string? LoadDocument() {
        var filter = GetFilter();
        var fileDialog = new OpenFileDialog {
            Multiselect = false,
            Filter = $"Documents({filter})|{filter}",
        };

        return fileDialog.ShowDialog() is true ? fileDialog.FileName : null;
    }

    private IEnumerable<string> LoadDocuments() {
        var folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) yield break;

        foreach (var file in Directory.EnumerateFiles(
                         folderDialog.SelectedPath,
                         "*.*",
                         SearchOption.AllDirectories)
                     .Where(file => _vm.Extensions.Exists(file.EndsWith))) {
            yield return file;
        }
    }

    private void SelectFile_OnClick(object sender, RoutedEventArgs e) {
        var parser = LoadDocument();
        if (parser is null) return;

        LaunchParser(parser);
    }

    private void SelectFolder_OnClick(object sender, RoutedEventArgs e) {
        var parsers = LoadDocuments();
        foreach (var parser in parsers) {
            LaunchParser(parser);
        }
    }

    private void LaunchParser(string filePath) {
        var dialogueVM = _vm.GetDialogueVM(filePath);
        var processDialogue = new ProcessDialogue(dialogueVM);
        processDialogue.ShowDialog();

        //Save warning
        if (dialogueVM.SavedSession
            || dialogueVM.DialogueTypeList.TrueForAll(s => s.SelectedTypes.Count == 0)) return;

        if (MessageBox.Show(
                "You didn't save your changes, do you want to save now?",
                string.Empty,
                MessageBoxButton.YesNo)
            == MessageBoxResult.Yes)
            dialogueVM.Save.Execute(null);
    }

    private void SelectionPythonPath_OnClick(object sender, RoutedEventArgs e) {
        const string filter = "*.dll";
        var fileDialog = new OpenFileDialog {
            Multiselect = false,
            Filter = $"Library({filter})|{filter}",
        };

        if (fileDialog.ShowDialog() is true) _vm.RefreshPython(fileDialog.FileName);
    }

    public void OpenOutput() {
        _vm.OutputPathProvider.CreateIfMissing();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo {
            FileName = $"\"{_vm.OutputPathProvider.OutputPath}\"",
            UseShellExecute = true,
            Verb = "open",
        };
        process.Start();
    }

    private void OpenFolder_OnClick(object sender, RoutedEventArgs e) {
        OpenOutput();
    }
}
