using System;
using DynamicData;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Extension;

public static class FormKeyExtensions {
    public static string ToFormID(this FormKey formKey, int loadOrderIndex) {
        if (loadOrderIndex is < 0 or > 255) {
            throw new ArgumentOutOfRangeException(nameof(loadOrderIndex), "Load order index must be between 0 and 255.");
        }

        return loadOrderIndex.ToString("D2") + formKey.IDString();
    }

    public static string ToFormID(this FormKey formKey, IModGetter mod, ILinkCache linkCache) {
        return formKey.ToFormID(linkCache.ListedOrder.IndexOf(mod));
    }
}
