using System.Windows;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.UI.Views;

public partial class FormKeySelectionWindow : Window {
    public FormKeySelectionWindow(string title, ILinkCache linkCache, IEnumerable<Type> types) {
        Title = title;
        LinkCache = linkCache;
        ScopedTypes = types;

        InitializeComponent();
    }

    public ILinkCache LinkCache { get; }
    public IEnumerable<Type> ScopedTypes { get; set; }
    public FormKey FormKey { get; set; }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        Close();
    }
}
