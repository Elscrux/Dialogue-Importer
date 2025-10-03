using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Services;

public interface IEnvironmentContext {
    public ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache { get; }
    public IGameEnvironment<ISkyrimMod, ISkyrimModGetter> Environment { get; }
    public SkyrimMod Mod { get; }
}
