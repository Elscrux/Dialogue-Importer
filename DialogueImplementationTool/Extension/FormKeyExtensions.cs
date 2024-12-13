using System;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
namespace DialogueImplementationTool.Extension;

public static class FormKeyExtensions {
    public static string ToFormID(this FormKey formKey, int loadOrderIndex) {
        if (loadOrderIndex is < 0 or > 255) {
            throw new ArgumentOutOfRangeException(
                nameof(loadOrderIndex),
                $"Load order index must be between 0 and 255 but is {loadOrderIndex}");
        }

        return loadOrderIndex.ToString("D2") + formKey.IDString();
    }

    public static string ToFormID(this FormKey formKey, ILinkCache linkCache) {
        var index = linkCache.ListedOrder.FindIndex<IModGetter, IModGetter>(x => x.ModKey == formKey.ModKey);
        return formKey.ToFormID(index);
    }
}
