using System;
using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Services;

public class InjectedFormKeySelection : IFormKeySelection {
    public FormKey GetFormKey(string title, IEnumerable<Type> types, FormKey defaultFormKey) {
        return defaultFormKey;
    }
}
