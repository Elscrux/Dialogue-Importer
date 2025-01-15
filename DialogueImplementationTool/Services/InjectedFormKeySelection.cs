using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Services;

public sealed class InjectedFormKeySelection : IFormKeySelection {
    private readonly Dictionary<string, FormKey> _injectedEntries = new();

    public InjectedFormKeySelection() {}
    public InjectedFormKeySelection(Dictionary<string, FormKey> injectedEntries) {
        _injectedEntries = injectedEntries;
    }

    public FormKey GetFormKey<TMajor>(string title, FormKey defaultFormKey) 
        where TMajor : IMajorRecordQueryableGetter => _injectedEntries.GetValueOrDefault(title, defaultFormKey);
}
