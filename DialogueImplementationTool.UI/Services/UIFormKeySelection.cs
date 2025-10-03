using System.Windows;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.UI.Services;

public sealed class UIFormKeySelection(
    EnvironmentContext environmentContext,
    AutoApplyProvider autoApplyProvider,
    FormKeyCache formKeyCache)
    : IFormKeySelection {
    private readonly Lock _lock = new();
    private readonly HashSet<string> _openedIdentifiers = [];

    public FormKey GetFormKey<TMajor>(string title, string identifier, FormKey defaultFormKey)
        where TMajor : IMajorRecordQueryableGetter {
        // Only show one form key selection window at a time
        lock (_lock) {
            var formKey = GetFormKeyImpl<TMajor>(title, identifier, defaultFormKey);
            formKeyCache.Set<TMajor>(identifier, formKey);
            return formKey;
        }
    }

    private FormKey GetFormKeyImpl<TMajor>(string title, string identifier, FormKey defaultFormKey)
        where TMajor : IMajorRecordQueryableGetter {
        if (formKeyCache.TryGetFormKey<TMajor>(identifier, out var formKey)) {
            defaultFormKey = formKey;

            // Don't prompt if it should auto apply, or if it's already been opened before
            if (autoApplyProvider.AutoApply || _openedIdentifiers.Contains(identifier)) {
                return formKey;
            }
        }

        formKey = Application.Current.Dispatcher.Invoke(() => {
            var formKeySelection = GetSelection();
            formKeySelection.ShowDialog();
            _openedIdentifiers.Add(identifier);

            while (formKeySelection.FormKey == FormKey.Null) {
                MessageBox.Show("You must select a form key");
                formKeySelection = GetSelection();
                formKeySelection.ShowDialog();
            }

            return formKeySelection.FormKey;

            FormKeySelectionWindow GetSelection() => new(title, environmentContext.LinkCache, [typeof(TMajor)], defaultFormKey);
        });

        return formKey;
    }
}
