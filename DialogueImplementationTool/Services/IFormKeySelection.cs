using System;
using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Services;

public interface IFormKeySelection {
    FormKey GetFormKey<TMajor>(string title, string identifier, FormKey defaultFormKey, bool canBeNull = false)
        where TMajor : IMajorRecordQueryableGetter;

    FormKey GetFormKey(string title, string identifier, FormKey defaultFormKey, bool canBeNull = false, params IReadOnlyList<Type> recordTypes);
}
