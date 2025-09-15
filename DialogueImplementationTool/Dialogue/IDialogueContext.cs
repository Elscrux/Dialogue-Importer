using System;
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
    List<string> Issues { get; }
    FormKey GetNextFormKey();
    void AddRecord<TMajorRecord>(TMajorRecord record) where TMajorRecord : IMajorRecord ;
    DialogTopic? GetTopic(string editorId);
    DialogTopic GetTopic(FormKey formKey);
    IDialogTopicGetter? GetTopic(DialogueTopic topic, Func<FormKey, DialogueTopic?> resolveIntermediateTopic);
    IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IReadOnlyList<string> speakerNames);
    IFormLink<IQuestGetter> GetFavorDialogueQuest();
    DialogBranch? GetServiceBranch(ServiceType serviceType, FormKey defaultBranchFormKey);
    public Condition? GetSpeakerCondition(ISpeaker speaker);
    TMajor SelectRecord<TMajor, TMajorGetter>(string prompt)
        where TMajor : class, TMajorGetter, IMajorRecordQueryable
        where TMajorGetter : class, IMajorRecordQueryableGetter;
    TMajor SelectRecord<TMajor, TMajorGetter>(string prompt, FormKey defaultFormKey)
        where TMajor : class, TMajorGetter, IMajorRecordQueryable
        where TMajorGetter : class, IMajorRecordQueryableGetter;
    TMajor GetOrAddOverride<TMajor, TMajorGetter>(IFormKeyGetter formKeyGetter)
        where TMajor : class, TMajorGetter, IMajorRecord
        where TMajorGetter : class, IMajorRecordGetter, IMajorRecordQueryableGetter;
    TMajorRecord GetOrAddRecord<TMajorRecord, TMajorRecordGetter>(string editorId, Func<TMajorRecord> recordFactory)
        where TMajorRecord : class, TMajorRecordGetter, IMajorRecord
        where TMajorRecordGetter : class, IMajorRecordGetter;
}
