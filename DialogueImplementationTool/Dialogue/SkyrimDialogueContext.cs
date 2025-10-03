using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
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
    IPrefixProvider prefixProvider,
    IEnvironmentContext environmentContext,
    IQuest quest,
    AutomaticSpeakerSelection automaticSpeakerSelection,
    ISpeakerSelection speakerSelection,
    AutoApplyProvider autoApplyProvider,
    IFormKeySelection formKeySelection)
    : IDialogueContext {
    private FormLink<IQuestGetter>? _favorDialogueQuest;

    public string Prefix => prefixProvider.Prefix;
    public SkyrimRelease Release => SkyrimRelease.SkyrimSE;
    public IGameEnvironment Environment { get; } = environmentContext.Environment;
    public ILinkCache LinkCache { get; } = environmentContext.LinkCache;
    public IQuest Quest { get; } = quest;
    public IMod Mod { get; } = environmentContext.Mod;
    public Dictionary<string, string> Scripts { get; } = [];
    public AutoApplyProvider AutoApplyProvider { get; } = autoApplyProvider;
    public List<string> Issues { get; } = [];

    public FormKey GetNextFormKey() {
        return environmentContext.Mod.GetNextFormKey();
    }

    public void AddRecord<TMajorRecord>(TMajorRecord record)
        where TMajorRecord : IMajorRecord {
        var group = environmentContext.Mod.GetTopLevelGroup<TMajorRecord>();
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

        var recordContext = environmentContext.LinkCache.ResolveContext<TMajorRecord, TMajorRecordGetter>(recordGetter.FormKey);
        return recordContext.GetOrAddAsOverride(environmentContext.Mod);
    }

    public DialogTopic? GetTopic(string editorId) {
        if (!environmentContext.LinkCache.TryResolveIdentifier<IDialogTopicGetter>(editorId, out var formKey)) return null;

        return GetTopic(formKey);
    }

    public DialogTopic GetTopic(FormKey formKey) {
        var topic = environmentContext.LinkCache.ResolveContext<DialogTopic, IDialogTopicGetter>(formKey);

        var overrideTopic = topic.GetOrAddAsOverride(environmentContext.Mod);

        // Add responses
        foreach (var response in topic.Record.Responses) {
            var responseContext =
                environmentContext.LinkCache.ResolveContext<IDialogResponses, IDialogResponsesGetter>(response.FormKey);
            responseContext.GetOrAddAsOverride(environmentContext.Mod);
        }

        return overrideTopic;
    }

    public IDialogTopicGetter? GetTopic(
        DialogueTopic dialogueTopic,
        Func<FormKey, DialogueTopic?> resolveIntermediateTopic,
        DialogueTopicInfo? currentWipTopicInfo = null) {
        foreach (var implementedTopic in environmentContext.LinkCache.PriorityOrder.WinningOverrides<IDialogTopicGetter>()) {
            if (implementedTopic.Quest.FormKey != Quest.FormKey) continue;

            if (CheckTopic(dialogueTopic, implementedTopic)) {
                return implementedTopic;
            }
        }

        return null;

        bool CheckTopic(DialogueTopic topic, IDialogTopicGetter implementedTopic, int depth = 0) {
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

                // Check speaker
                var speakerCondition = GetSpeakerCondition(topicInfo.Speaker);
                if (!implementedTopicInfo.Conditions.Contains(speakerCondition)) return false;

                // Check conditions - expect exactly one condition for the speaker and the rest to be extra conditions
                if (topicInfo.ExtraConditions.Any(c => !implementedTopicInfo.Conditions.Contains(c))) return false;

                // Check responses
                if (implementedTopicInfo.ResponseData.IsNull != topicInfo.SharedInfo is null) return false;

                // Check shared info
                if (!CheckSharedInfo(topicInfo, implementedTopicInfo)) return false;

                // Check links
                if (!CheckLinks(topicInfo, implementedTopicInfo)) return false;
            }

            return true;

            bool CheckResponses(DialogueTopicInfo topicInfo, IDialogResponsesGetter implementedTopicInfo) {
                for (var responseIndex = 0; responseIndex < topicInfo.Responses.Count; responseIndex++) {
                    var response = topicInfo.Responses[responseIndex];
                    var implementedResponse = implementedTopicInfo.Responses[responseIndex];

                    if (!string.Equals(response.FullResponse, implementedResponse.Text.String, StringComparison.Ordinal))
                        return false;
                }

                return true;
            }

            bool CheckSharedInfo(DialogueTopicInfo topicInfo, IDialogResponsesGetter implementedTopicInfo) {
                if (topicInfo.SharedInfo is null) {
                    if (topicInfo.Responses.Count != implementedTopicInfo.Responses.Count) return false;

                    if (!CheckResponses(topicInfo, implementedTopicInfo)) return false;
                } else if (!LinkCache.TryResolve<IDialogResponsesGetter>(
                        implementedTopicInfo.ResponseData.FormKey,
                        out var sharedInfo)
                 || sharedInfo.Responses.Count != topicInfo.SharedInfo.ResponseDataTopicInfo.Responses.Count
                 || !sharedInfo.Responses.Select(x => x.Text.String)
                        .SequenceEqual(topicInfo.SharedInfo.ResponseDataTopicInfo.Responses.Select(x => x.FullResponse))) {
                    return false;
                }

                return true;
            }

            bool CheckLinks(DialogueTopicInfo topicInfo, IDialogResponsesGetter implementedTopicInfo) {
                // Prevent infinite recursion
                if (depth > 1) return true;

                if (topicInfo.Equals(currentWipTopicInfo)) return true;
                if (topicInfo.Links.Count != implementedTopicInfo.LinkTo.Count) return false;

                for (var i = 0; i < topicInfo.Links.Count; i++) {
                    var link = topicInfo.Links[i];
                    var linkedTopic = implementedTopicInfo.LinkTo[i].TryResolve(LinkCache);

                    if (linkedTopic is null) {
                        // In case the linked topic wasn't implemented yet, check the intermediate topic
                        var intermediateTopic = resolveIntermediateTopic(implementedTopicInfo.LinkTo[i].FormKey);

                        // Assume when we can't find the topic at all that it's probably the right one
                        if (intermediateTopic is null) continue;

                        if (!link.Equals(intermediateTopic)) return false;
                    } else {
                        if (!CheckTopic(link, linkedTopic, depth + 1)) return false;
                    }
                }

                return true;
            }
        }
    }

    public IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IReadOnlyList<string> speakerNames) {
        if (AutoApplyProvider.AutoApply) {
            var automaticSpeakers = automaticSpeakerSelection.GetSpeakers<AliasSpeaker>(speakerNames);
            if (automaticSpeakers.Count == speakerNames.Count) return automaticSpeakers;
        }

        return speakerSelection.GetSpeakers<AliasSpeaker>(speakerNames);
    }

    public IFormLink<IQuestGetter> GetFavorDialogueQuest() {
        if (_favorDialogueQuest is not null) return _favorDialogueQuest;

        var formKey = formKeySelection.GetFormKey<IQuestGetter>(
                "Select the favor dialogue quest",
                "Favor Dialogue Quest",
                Skyrim.Quest.DialogueFavorGeneric.FormKey);

        _favorDialogueQuest = formKey.ToLink<IQuestGetter>();
        return _favorDialogueQuest;
    }

    public DialogBranch GetServiceBranch(ServiceType serviceType, FormKey defaultBranchFormKey) {
        var formKey = formKeySelection.GetFormKey<IDialogBranchGetter>(
            $"Select the {serviceType} branch",
            serviceType.ToString(),
            defaultBranchFormKey);

        var context = environmentContext.LinkCache.ResolveContext<DialogBranch, IDialogBranchGetter>(formKey);
        return context.GetOrAddAsOverride(environmentContext.Mod);
    }

    public TMajor SelectRecord<TMajor, TMajorGetter>(string prompt)
        where TMajor : class, TMajorGetter, IMajorRecordQueryable
        where TMajorGetter : class, IMajorRecordQueryableGetter {
        return SelectRecord<TMajor, TMajorGetter>(prompt, FormKey.Null);
    }

    public TMajor SelectRecord<TMajor, TMajorGetter>(string prompt, FormKey defaultFormKey)
        where TMajor : class, TMajorGetter, IMajorRecordQueryable
        where TMajorGetter : class, IMajorRecordQueryableGetter {
        var formKey = formKeySelection.GetFormKey<TMajorGetter>($"Select: {prompt}", prompt, defaultFormKey);

        var context = environmentContext.LinkCache.ResolveContext<TMajor, TMajorGetter>(formKey);
        return context.GetOrAddAsOverride(environmentContext.Mod);
    }

    public TMajor GetOrAddOverride<TMajor, TMajorGetter>(IFormKeyGetter formKeyGetter)
        where TMajor : class, TMajorGetter, IMajorRecord
        where TMajorGetter : class, IMajorRecordGetter, IMajorRecordQueryableGetter {
        var context = environmentContext.LinkCache.ResolveContext<TMajor, TMajorGetter>(formKeyGetter.FormKey);
        return context.GetOrAddAsOverride(environmentContext.Mod);
    }

    public Condition? GetSpeakerCondition(ISpeaker speaker) {
        if (speaker is AliasSpeaker aliasSpeaker) {
            return new GetIsAliasRefConditionData {
                ReferenceAliasIndex = aliasSpeaker.AliasIndex,
            }.ToConditionFloat();
        }

        if (LinkCache.TryResolve<INpcGetter>(speaker.FormLink.FormKey, out var npc)) {
            return new GetIsIDConditionData {
                Object = {
                    Link = { FormKey = npc.FormKey }
                }
            }.ToConditionFloat();
        }

        if (LinkCache.TryResolve<IFactionGetter>(speaker.FormLink.FormKey, out var faction)) {
            return new GetInFactionConditionData {
                Faction = { Link = { FormKey = faction.FormKey } }
            }.ToConditionFloat();
        }

        if (LinkCache.TryResolve<IVoiceTypeGetter>(speaker.FormLink.FormKey, out var voiceType)) {
            return new GetIsVoiceTypeConditionData {
                VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } }
            }.ToConditionFloat();
        }

        if (LinkCache.TryResolve<IFormListGetter>(speaker.FormLink.FormKey, out var formList)) {
            return new GetIsVoiceTypeConditionData {
                VoiceTypeOrList = { Link = { FormKey = formList.FormKey } }
            }.ToConditionFloat();
        }

        if (LinkCache.TryResolve<ITalkingActivatorGetter>(speaker.FormLink.FormKey, out var talkingActivator)) {
            return new GetIsIDConditionData {
                Object = { Link = { FormKey = talkingActivator.FormKey } }
            }.ToConditionFloat();
        }

        return null;
    }
}
