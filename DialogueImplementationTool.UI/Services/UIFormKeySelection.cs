using System.Windows;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.UI.Services;

public sealed class UIFormKeySelection(ILinkCache linkCache, AutoApplyProvider autoApplyProvider) : IFormKeySelection {
    private static readonly Dictionary<string, FormKey> FormKeyCache = new();

    public FormKey GetFormKey(string title, IReadOnlyList<Type> types, FormKey defaultFormKey) {
        if (FormKeyCache.TryGetValue(title, out var formKey)) {
            defaultFormKey = formKey;

            // Don't prompt if it should auto apply
            if (autoApplyProvider.AutoApply) {
                return formKey;
            }
        }

        var formKeySelection = GetSelection();

        formKeySelection.ShowDialog();

        while (formKeySelection.FormKey == FormKey.Null) {
            MessageBox.Show("You must select a form key");
            formKeySelection = GetSelection();
            formKeySelection.ShowDialog();
        }

        FormKeyCache[title] = formKeySelection.FormKey;
        return formKeySelection.FormKey;

        FormKeySelectionWindow GetSelection() => new(title, linkCache, types, defaultFormKey);
    }
}
