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
    private readonly object _lock = new();
    private readonly HashSet<string> _openedTitles = [];

    public FormKey GetFormKey<TMajor>(string title, FormKey defaultFormKey)
        where TMajor : IMajorRecordQueryableGetter {
        // Only show one form key selection window at a time
        lock (_lock) {
            return GetFormKeyImpl<TMajor>(title, defaultFormKey);
        }
    }

    private FormKey GetFormKeyImpl<TMajor>(string title, FormKey defaultFormKey)
        where TMajor : IMajorRecordQueryableGetter {
        if (formKeyCache.TryGetFormKey<TMajor>(title, out var formKey)) {
            defaultFormKey = formKey;

            // Don't prompt if it should auto apply, or if it's already been opened before
            if (autoApplyProvider.AutoApply || _openedTitles.Contains(title)) {
                return formKey;
            }
        }

        formKey = Application.Current.Dispatcher.Invoke(() => {
            var formKeySelection = GetSelection();
            formKeySelection.ShowDialog();
            _openedTitles.Add(title);

            while (formKeySelection.FormKey == FormKey.Null) {
                MessageBox.Show("You must select a form key");
                formKeySelection = GetSelection();
                formKeySelection.ShowDialog();
            }

            return formKeySelection.FormKey;

            FormKeySelectionWindow GetSelection() => new(title, environmentContext.LinkCache, [typeof(TMajor)], defaultFormKey);
        });

        formKeyCache.Set<TMajor>(title, formKey);
        return formKey;
    }
}
