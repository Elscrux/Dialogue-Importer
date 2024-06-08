using System.Windows;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.UI.Services;

public sealed class UIFormKeySelection(ILinkCache linkCache) : IFormKeySelection {
    public FormKey GetFormKey(string title, IReadOnlyList<Type> types, FormKey defaultFormKey) {
        var formKeySelection = GetSelection();

        formKeySelection.ShowDialog();

        while (formKeySelection.FormKey == FormKey.Null) {
            MessageBox.Show("You must select a form key");
            formKeySelection = GetSelection();
            formKeySelection.ShowDialog();
        }

        return formKeySelection.FormKey;

        FormKeySelectionWindow GetSelection() => new(title, linkCache, types, defaultFormKey);
    }
}
