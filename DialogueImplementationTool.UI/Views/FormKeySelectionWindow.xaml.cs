using System.Windows;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.UI.Views;

public partial class FormKeySelectionWindow {
    public FormKeySelectionWindow(string title, ILinkCache linkCache, IEnumerable<Type> types) : this(title,
        linkCache,
        types,
        FormKey.Null) {}

    public FormKeySelectionWindow(string title, ILinkCache linkCache, IEnumerable<Type> types, FormKey defaultFormKey) {
        Title = title;

        InitializeComponent();

        FormKeyPicker.LinkCache = linkCache;
        FormKeyPicker.ScopedTypes = types;
        FormKeyPicker.FormKey = FormKey = defaultFormKey;
    }

    public FormKey FormKey { get; set; }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        FormKey = FormKeyPicker.FormKey;
        Close();
    }
}
