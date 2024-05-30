using System;
using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Services;

public interface IFormKeySelection {
    FormKey GetFormKey(string title, IEnumerable<Type> types, FormKey defaultFormKey);
}
