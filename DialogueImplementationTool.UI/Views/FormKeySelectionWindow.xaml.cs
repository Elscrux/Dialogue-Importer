using System.Windows;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.UI.Views;

public partial class FormKeySelectionWindow {
    private static readonly Dictionary<string, FormKey> FormKeyCache = new();

    public FormKeySelectionWindow(string title, ILinkCache linkCache, IEnumerable<Type> types) : this(title,
        linkCache,
        types,
        FormKey.Null) {}

    public FormKeySelectionWindow(string title, ILinkCache linkCache, IEnumerable<Type> types, FormKey defaultFormKey) {
        Title = title;

        InitializeComponent();

        FormKeyPicker.LinkCache = linkCache;
        FormKeyPicker.ScopedTypes = types;

        if (FormKeyCache.TryGetValue(Title, out var formKey)) defaultFormKey = formKey;
        FormKeyPicker.FormKey = FormKey = defaultFormKey;
    }

    public FormKey FormKey { get; set; }

    private void Save_OnClick(object sender, RoutedEventArgs e) {
        FormKey = FormKeyPicker.FormKey;
        FormKeyCache[Title] = FormKey;
        Close();
    }
}
