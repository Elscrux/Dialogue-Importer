using System.Windows;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.UI.Services;

public sealed class UIFormKeySelection(EnvironmentContext environmentContext, AutoApplyProvider autoApplyProvider)
    : IFormKeySelection {
    private readonly Dictionary<string, FormKey> _formKeyCache = new();
    private readonly object _lock = new();

    public FormKey GetFormKey(string title, IReadOnlyList<Type> types, FormKey defaultFormKey) {
        // Only show one form key selection window at a time
        lock (_lock) {
            return GetFormKeyImpl(title, types, defaultFormKey);
        }
    }

    private FormKey GetFormKeyImpl(string title, IReadOnlyList<Type> types, FormKey defaultFormKey) {
        if (_formKeyCache.TryGetValue(title, out var formKey)) {
            defaultFormKey = formKey;

            // Don't prompt if it should auto apply
            if (autoApplyProvider.AutoApply) {
                return formKey;
            }
        }

        formKey = Application.Current.Dispatcher.Invoke(() => {
            var formKeySelection = GetSelection();
            formKeySelection.ShowDialog();

            while (formKeySelection.FormKey == FormKey.Null) {
                MessageBox.Show("You must select a form key");
                formKeySelection = GetSelection();
                formKeySelection.ShowDialog();
            }

            return formKeySelection.FormKey;

            FormKeySelectionWindow GetSelection() => new(title, environmentContext.LinkCache, types, defaultFormKey);
        });

        _formKeyCache[title] = formKey;
        return formKey;
    }
}
