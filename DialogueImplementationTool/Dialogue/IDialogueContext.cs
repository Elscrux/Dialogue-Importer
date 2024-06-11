using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public interface IDialogueContext {
    string Prefix { get; }
    SkyrimRelease Release { get; }
    IGameEnvironment Environment { get; }
    ILinkCache LinkCache { get; }
    IQuest Quest { get; }
    IMod Mod { get; }
    Dictionary<string, string> Scripts { get; }
    AutoApplyProvider AutoApplyProvider { get; }
    FormKey GetNextFormKey();
    void AddScene(Scene scene);
    void AddQuest(Quest quest);
    void AddDialogBranch(DialogBranch branch);
    void AddDialogTopic(DialogTopic topic);
    DialogTopic? GetTopic(string editorId);
    DialogTopic GetTopic(FormKey formKey);
    IDialogTopicGetter? GetTopic(DialogueTopic topic);
    IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IReadOnlyList<string> speakerNames);
    IFormLink<IQuestGetter> GetFavorDialogueQuest();
    DialogBranch? GetServiceBranch(ServiceType serviceType, FormKey defaultBranchFormKey);
    TMajor SelectRecord<TMajor, TMajorGetter>(string prompt)
        where TMajor : class, TMajorGetter, IMajorRecordQueryable
        where TMajorGetter : class, IMajorRecordQueryableGetter;
}
