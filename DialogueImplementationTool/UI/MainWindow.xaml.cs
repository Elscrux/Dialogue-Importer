using System.Windows;
using DialogueImplementationTool.Parser;

namespace DialogueImplementationTool.UI;

public partial class MainWindow {
   
    
    public MainWindow() {
        InitializeComponent();
    }
    
    private void SelectFile_OnClick(object sender, RoutedEventArgs e) {
        var documentParser = DocumentParser.LoadDocument();
        if (documentParser == null) {
            MessageBox.Show("Document couldn't be loaded!");
            return;
        }

        new TypeSelectionWindow(documentParser).ShowDialog();
    }
    private void SelectFolder_OnClick(object sender, RoutedEventArgs e) {
        throw new System.NotImplementedException();
    }
}