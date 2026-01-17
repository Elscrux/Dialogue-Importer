using System.IO;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Extension;

public static class ModExtension {
    public static void Save(
        this IMod mod,
        string directoryPath,
        IGameEnvironment env) {
        var directoryInfo = new DirectoryInfo(Path.Combine(directoryPath, mod.ModKey.Name));
        var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, mod.ModKey.FileName));

        if (fileInfo.Directory is { Exists: false }) fileInfo.Directory?.Create();

        mod.BeginWrite
            .ToPath(fileInfo.FullName)
            .WithLoadOrder(env.LoadOrder)
            // TODO replace with other method of getting master infos when using a decentralized data folder
            .WithDataFolder(env.DataFolderPath)
            .WithAllParentMasters()
            .Write();
    }
}
