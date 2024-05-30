using System.Windows;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.UI.Services;

public sealed class UIFormKeySelection(ILinkCache linkCache) : IFormKeySelection {
    public FormKey GetFormKey(string title, IEnumerable<Type> types, FormKey defaultFormKey) {
        var formKeySelection = new FormKeySelectionWindow(title, linkCache, types) {
            FormKey = defaultFormKey,
        };

        formKeySelection.ShowDialog();

        while (formKeySelection.FormKey == FormKey.Null) {
            MessageBox.Show("You must select a form key");
            formKeySelection.ShowDialog();
        }

        return formKeySelection.FormKey;
    }
}
