using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class SkyrimDialogueContext(
    string prefix,
    IGameEnvironment<ISkyrimMod, ISkyrimModGetter> environment,
    ISkyrimMod mod,
    IQuest quest,
    ISpeakerSelection speakerSelection,
    AutoApplyProvider autoApplyProvider,
    ISpeakerFavoritesSelection speakerFavoritesSelection,
    IFormKeySelection formKeySelection)
    : IDialogueContext {
    private readonly AutomaticSpeakerSelection _automaticSpeakerSelection =
        new(environment.LinkCache, speakerFavoritesSelection);
    private FormLink<IQuestGetter>? _favorDialogueQuest;

    public string Prefix { get; } = prefix;
    public SkyrimRelease Release => SkyrimRelease.SkyrimSE;
    public IGameEnvironment Environment { get; } = environment;
    public ILinkCache LinkCache { get; } = environment.LinkCache;
    public IQuest Quest { get; } = quest;
    public IMod Mod { get; } = mod;
    public Dictionary<string, string> Scripts { get; } = [];
    public AutoApplyProvider AutoApplyProvider { get; } = autoApplyProvider;
    public List<string> Issues { get; } = [];

    public FormKey GetNextFormKey() {
        return mod.GetNextFormKey();
    }

    public void AddRecord<TMajorRecord>(TMajorRecord record)
        where TMajorRecord : IMajorRecord {
        var group = mod.GetTopLevelGroup<TMajorRecord>();
        if (!group.ContainsKey(record.FormKey)) {
            group.Add(record);
        }
    }

    public TMajorRecord GetOrAddRecord<TMajorRecord, TMajorRecordGetter>(string editorId, Func<TMajorRecord> recordFactory)
        where TMajorRecord : class, TMajorRecordGetter, IMajorRecord
        where TMajorRecordGetter : class, IMajorRecordGetter {
        var recordGetter = Environment.LinkCache.PriorityOrder.WinningOverrides<TMajorRecordGetter>()
            .FirstOrDefault(q => q.EditorID == editorId);
        if (recordGetter is null) {
            var record = recordFactory();
            AddRecord(record);
            return record;
        }

        var recordContext = environment.LinkCache.ResolveContext<TMajorRecord, TMajorRecordGetter>(recordGetter.FormKey);
        return recordContext.GetOrAddAsOverride(mod);
    }

    public DialogTopic? GetTopic(string editorId) {
        if (!environment.LinkCache.TryResolveIdentifier<IDialogTopicGetter>(editorId, out var formKey)) return null;

        return GetTopic(formKey);
    }

    public DialogTopic GetTopic(FormKey formKey) {
        var topic = environment.LinkCache.ResolveContext<DialogTopic, IDialogTopicGetter>(formKey);

        var overrideTopic = topic.GetOrAddAsOverride(mod);

        // Add responses
        foreach (var response in topic.Record.Responses) {
            var responseContext =
                environment.LinkCache.ResolveContext<IDialogResponses, IDialogResponsesGetter>(response.FormKey);
            responseContext.GetOrAddAsOverride(mod);
        }

        return overrideTopic;
    }

    public IDialogTopicGetter? GetTopic(DialogueTopic topic) {
        foreach (var implementedTopic in environment.LinkCache.PriorityOrder.WinningOverrides<IDialogTopicGetter>()) {
            if (implementedTopic.Quest.FormKey != Quest.FormKey) continue;

            if (Matches(implementedTopic)) {
                return implementedTopic;
            }
        }

        return null;

        bool Matches(IDialogTopicGetter implementedTopic) {
            var playerText = topic.GetPlayerFullText();
            if (playerText != string.Empty && playerText != "(invis cont)"
             && playerText != implementedTopic.Name?.String) return false;

            if (topic.TopicInfos.Count != implementedTopic.Responses.Count) return false;

            for (var topicInfoIndex = 0; topicInfoIndex < topic.TopicInfos.Count; topicInfoIndex++) {
                var topicInfo = topic.TopicInfos[topicInfoIndex];
                var implementedTopicInfo = implementedTopic.Responses[topicInfoIndex];

                // Check prompt
                if (playerText == string.Empty && topicInfo.Prompt.FullText != implementedTopicInfo.Prompt?.String)
                    return false;

                // Check shared info
                if (topicInfo.SharedInfo is null != implementedTopicInfo.ResponseData.IsNull) return false;

                // Check flags
                if (topicInfo.InvisibleContinue
                 != implementedTopicInfo.Flags?.Flags.HasFlag(DialogResponses.Flag.InvisibleContinue)) return false;
                if (topicInfo.Goodbye != implementedTopicInfo.Flags?.Flags.HasFlag(DialogResponses.Flag.Goodbye)) return false;
                if (topicInfo.Random != implementedTopicInfo.Flags?.Flags.HasFlag(DialogResponses.Flag.Random)) return false;
                if (topicInfo.SayOnce != implementedTopicInfo.Flags?.Flags.HasFlag(DialogResponses.Flag.SayOnce)) return false;

                // Check conditions - expect exactly one condition for the speaker and the rest to be extra conditions
                if (topicInfo.ExtraConditions.Any(c => !implementedTopicInfo.Conditions.Contains(c))) return false;

                // Check responses
                if (implementedTopicInfo.ResponseData.IsNull != topicInfo.SharedInfo is null) return false;

                if (topicInfo.SharedInfo is null) {
                    if (topicInfo.Responses.Count != implementedTopicInfo.Responses.Count) return false;

                    for (var responseIndex = 0; responseIndex < topicInfo.Responses.Count; responseIndex++) {
                        var response = topicInfo.Responses[responseIndex];
                        var implementedResponse = implementedTopicInfo.Responses[responseIndex];

                        if (!string.Equals(response.FullResponse, implementedResponse.Text.String, StringComparison.Ordinal))
                            return false;
                    }
                } else if (LinkCache.TryResolve<IDialogResponsesGetter>(
                        implementedTopicInfo.ResponseData.FormKey,
                        out var sharedInfo)
                 && sharedInfo.Responses.Count != topicInfo.SharedInfo.ResponseDataTopicInfo.Responses.Count) {
                    return false;
                }
            }

            return true;
        }
    }

    public IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IReadOnlyList<string> speakerNames) {
        if (AutoApplyProvider.AutoApply) {
            var automaticSpeakers = _automaticSpeakerSelection.GetSpeakers<AliasSpeaker>(speakerNames);
            if (automaticSpeakers.Count == speakerNames.Count) return automaticSpeakers;
        }

        return speakerSelection.GetSpeakers<AliasSpeaker>(speakerNames);
    }

    public IFormLink<IQuestGetter> GetFavorDialogueQuest() {
        if (_favorDialogueQuest is not null) return _favorDialogueQuest;

        var formKey =
            formKeySelection.GetFormKey<IQuestGetter>(
                "Select the favor dialogue quest",
                Skyrim.Quest.DialogueFavorGeneric.FormKey);

        _favorDialogueQuest = formKey.ToLink<IQuestGetter>();
        return _favorDialogueQuest;
    }

    public DialogBranch GetServiceBranch(ServiceType serviceType, FormKey defaultBranchFormKey) {
        var formKey =
            formKeySelection.GetFormKey<IDialogBranchGetter>(
                $"Select the {serviceType} branch",
                defaultBranchFormKey);

        var context = environment.LinkCache.ResolveContext<DialogBranch, IDialogBranchGetter>(formKey);
        return context.GetOrAddAsOverride(mod);
    }

    public TMajor SelectRecord<TMajor, TMajorGetter>(string prompt)
        where TMajor : class, TMajorGetter, IMajorRecordQueryable
        where TMajorGetter : class, IMajorRecordQueryableGetter {
        return SelectRecord<TMajor, TMajorGetter>(prompt, FormKey.Null);
    }

    public TMajor SelectRecord<TMajor, TMajorGetter>(string prompt, FormKey defaultFormKey)
        where TMajor : class, TMajorGetter, IMajorRecordQueryable
        where TMajorGetter : class, IMajorRecordQueryableGetter {
        var formKey = formKeySelection.GetFormKey<TMajorGetter>($"Select: {prompt}", defaultFormKey);

        var context = environment.LinkCache.ResolveContext<TMajor, TMajorGetter>(formKey);
        return context.GetOrAddAsOverride(mod);
    }

    public TMajor GetOrAddOverride<TMajor, TMajorGetter>(IFormKeyGetter formKeyGetter)
        where TMajor : class, TMajorGetter, IMajorRecord
        where TMajorGetter : class, IMajorRecordGetter, IMajorRecordQueryableGetter {
        var context = environment.LinkCache.ResolveContext<TMajor, TMajorGetter>(formKeyGetter.FormKey);
        return context.GetOrAddAsOverride(mod);
    }
}
