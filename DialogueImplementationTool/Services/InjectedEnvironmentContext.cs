using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Services;

public sealed record InjectedEnvironmentContext(IGameEnvironment<ISkyrimMod, ISkyrimModGetter> Environment, SkyrimMod Mod) : IEnvironmentContext {
    public ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache => Environment.LinkCache;
}
