using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class SkyrimDialogueContext(
    ILinkCache<ISkyrimMod, ISkyrimModGetter> linkCache,
    ISkyrimMod mod,
    IQuest quest,
    ISpeakerSelection speakerSelection)
    : IDialogueContext {
    public SkyrimRelease Release => SkyrimRelease.SkyrimSE;
    public ILinkCache LinkCache { get; } = linkCache;
    public IQuest Quest { get; } = quest;
    public IMod Mod { get; } = mod;

    public FormKey GetNextFormKey() {
        return mod.GetNextFormKey();
    }

    public void AddScene(Scene scene) {
        if (!mod.Scenes.ContainsKey(scene.FormKey)) mod.Scenes.Add(scene);
    }

    public void AddQuest(Quest quest) {
        if (!mod.Quests.ContainsKey(quest.FormKey)) mod.Quests.Add(quest);
    }

    public void AddDialogBranch(DialogBranch branch) {
        if (!mod.DialogBranches.ContainsKey(branch.FormKey)) mod.DialogBranches.Add(branch);
    }

    public void AddDialogTopic(DialogTopic topic) {
        if (!mod.DialogTopics.ContainsKey(topic.FormKey)) mod.DialogTopics.Add(topic);
    }

    public DialogTopic? GetTopic(string editorId) {
        if (!linkCache.TryResolveIdentifier<IDialogTopicGetter>(editorId, out var formKey)) return null;

        return GetTopic(formKey);
    }

    public DialogTopic GetTopic(FormKey formKey) {
        var topic = linkCache.ResolveContext<DialogTopic, IDialogTopicGetter>(formKey);
        return topic.GetOrAddAsOverride(mod);
    }

    public IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IEnumerable<string> speakerNames) {
        return speakerSelection.GetAliasSpeakers(speakerNames);
    }
}
