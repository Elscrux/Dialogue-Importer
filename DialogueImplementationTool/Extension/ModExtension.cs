using System.IO;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Extension;

public static class ModExtension {
    public static void Save(
        this IMod mod,
        string directoryPath,
        ILoadOrderGetter<IModListingGetter<IModFlagsGetter>> loadOrder) {
        var directoryInfo = new DirectoryInfo(Path.Combine(directoryPath, mod.ModKey.Name));
        var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, mod.ModKey.FileName));

        if (fileInfo.Directory is { Exists: false }) fileInfo.Directory?.Create();

        mod.BeginWrite
            .ToPath(fileInfo.FullName)
            .WithLoadOrder(loadOrder)
            .WithExtraIncludedMasters(
                Skyrim.ModKey,
                Update.ModKey,
                Dawnguard.ModKey,
                HearthFires.ModKey,
                Dragonborn.ModKey)
            .Write();
    }
}
