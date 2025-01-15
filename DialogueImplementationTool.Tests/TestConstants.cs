using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests;

public sealed class TestConstants {
    private const SkyrimRelease Release = SkyrimRelease.SkyrimSE;

    public TestConstants() {
        Environment = GameEnvironment.Typical
            .Builder<ISkyrimMod, ISkyrimModGetter>(GameRelease.SkyrimSE)
            .WithOutputMod(Mod)
            .Build();

        Speaker1 = new NpcSpeaker(LinkCache, FormKey.Factory("111111:TestMod.esp"));
        Speaker2 = new NpcSpeaker(LinkCache, FormKey.Factory("222222:TestMod.esp"));
        Speaker3 = new NpcSpeaker(LinkCache, FormKey.Factory("333333:TestMod.esp"));

        Quest = new Quest(FormKey.Factory("999999:TestMod.esp"), Release) { EditorID = "TestQuest" };

        Mod.Quests.Add(Quest);
        Mod.Npcs.AddNew(Speaker1.FormKey);
        Mod.Npcs.AddNew(Speaker2.FormKey);
        Mod.Npcs.AddNew(Speaker3.FormKey);
    }

    public SkyrimMod Mod { get; } = new(ModKey.FromName("TestMod.esp", ModType.Plugin), SkyrimRelease.SkyrimSE);

    public IGameEnvironment<ISkyrimMod, ISkyrimModGetter> Environment { get; }

    public ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache => Environment.LinkCache;

    public ISpeakerSelection SpeakerSelection { get; set; } =
        new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker>());

    public IFormKeySelection FormKeySelection { get; set; } =
        new InjectedFormKeySelection();

    public SkyrimDialogueContext SkyrimDialogueContext => new(
        string.Empty,
        Environment,
        Mod,
        Quest,
        SpeakerSelection,
        new AutoApplyProvider(),
        new SpeakerFavoritesSelection(),
        FormKeySelection);

    public Quest Quest { get; }
    public NpcSpeaker Speaker1 { get; }
    public NpcSpeaker Speaker2 { get; }
    public NpcSpeaker Speaker3 { get; }
    public DialogueProcessor DialogueProcessor => new(SkyrimDialogueContext, new NullEmotionClassifierProvider());
}
