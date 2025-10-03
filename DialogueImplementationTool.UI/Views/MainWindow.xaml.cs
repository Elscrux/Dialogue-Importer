using System.Diagnostics;
using System.Windows;
using DialogueImplementationTool.UI.ViewModels;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
namespace DialogueImplementationTool.UI.Views;

public partial class MainWindow {
    private readonly MainWindowVM _vm = null!;

    public MainWindow() {
        InitializeComponent();
    }

    public MainWindow(MainWindowVM vm) {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    private void SelectionPythonPath_OnClick(object sender, RoutedEventArgs e) {
        const string filter = "*.dll";
        var fileDialog = new OpenFileDialog {
            Multiselect = false,
            Filter = $"Library({filter})|{filter}",
        };

        if (fileDialog.ShowDialog() is true) _vm.PythonEmotionClassifierProvider?.RefreshPython(fileDialog.FileName);
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

    private void AddQuest_OnClick(object sender, RoutedEventArgs e) {
        if (QuestPicker.FormKey.IsNull) return;

        _vm.AddQuest(QuestPicker.FormKey);
    }
}
