using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using DialogueImplementationTool.UI.ViewModels;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
namespace DialogueImplementationTool.UI.Views;

public partial class MainWindow {
    private readonly MainWindowVM _vm;

    public MainWindow() {
        InitializeComponent();
    }

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

        _vm.AddDocument(parser);
    }

    private void SelectFolder_OnClick(object sender, RoutedEventArgs e) {
        var parsers = LoadDocuments();
        foreach (var parser in parsers) {
            _vm.AddDocument(parser);
        }
    }

    private void SelectionPythonPath_OnClick(object sender, RoutedEventArgs e) {
        const string filter = "*.dll";
        var fileDialog = new OpenFileDialog {
            Multiselect = false,
            Filter = $"Library({filter})|{filter}",
        };

        if (fileDialog.ShowDialog() is true) _vm.PythonEmotionClassifierProvider.RefreshPython(fileDialog.FileName);
    }

    private void OpenOutput() {
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
