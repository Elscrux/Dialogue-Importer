using System.IO;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Extension;

public static class ModExtension {
    public static void Save(this IMod mod, string directoryPath) {
        var directoryInfo = new DirectoryInfo(Path.Combine(directoryPath, mod.ModKey.Name));
        var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, mod.ModKey.FileName));

        if (fileInfo.Directory is { Exists: false }) fileInfo.Directory?.Create();
        mod.WriteToBinary(fileInfo.FullName);
    }
}
