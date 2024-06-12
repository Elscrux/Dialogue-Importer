using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Services;

public class InjectedFormKeySelection : IFormKeySelection {
    public FormKey GetFormKey(string title, IReadOnlyList<Type> types, FormKey defaultFormKey) {
        return defaultFormKey;
    }
}
