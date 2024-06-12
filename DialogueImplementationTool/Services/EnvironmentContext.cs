using System.IO;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Services;

public sealed class EnvironmentContext {
    public ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache => Environment.LinkCache;
    public IGameEnvironment<ISkyrimMod, ISkyrimModGetter> Environment { get; }
    public SkyrimMod Mod { get; }

    public EnvironmentContext(OutputPathProvider outputPathProvider) {
        Mod = new SkyrimMod(new ModKey(GetNewModName(outputPathProvider), ModType.Plugin), SkyrimRelease.SkyrimSE, 1.7f);
        Environment = GameEnvironmentBuilder<ISkyrimMod, ISkyrimModGetter>.Create(GameRelease.SkyrimSE)
            .WithOutputMod(Mod)
            .Build();
    }

    private static string GetNewModName(OutputPathProvider outputPathProvider) {
        outputPathProvider.CreateIfMissing();

        const string modName = "DialogueOutput";
        var index = 1;
        var fileInfo = new DirectoryInfo(Path.Combine(outputPathProvider.OutputPath, $"{modName}{index}"));
        while (fileInfo.Exists) {
            index++;
            fileInfo = new DirectoryInfo(Path.Combine(outputPathProvider.OutputPath, $"{modName}{index}"));
        }

        return modName + index;
    }
}
