using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
using Noggog;
using Condition = Mutagen.Bethesda.Skyrim.Condition;
namespace DialogueImplementationTool.Dialogue;

public abstract partial class SceneFactory(IDialogueContext context) : DialogueFactory(context) {
    private List<int> _aliasIndices = [];
    protected IReadOnlyList<AliasSpeaker> AliasSpeakers = [];
    protected List<(FormKey FormKey, List<AliasSpeaker> Speakers)> NameMappedSpeakers = [];

    public override void GenerateDialogue(IQuest quest, List<DialogueTopic> topics) {
        var scene = GetCurrentScene(quest);
        if (scene is null) return;

        //Add lines
        AddLines(quest, scene, topics.ToList());
    }

    protected abstract Scene? GetCurrentScene(IQuest quest);

    private void AddLines(
        IQuest quest,
        Scene scene,
        List<DialogueTopic> topics) {
        uint currentPhaseIndex = 0;

        scene.LastActionIndex ??= 1;

        foreach (var topic in topics) {
            var aliasSpeaker = GetSpeaker(topic.TopicInfos[0].Speaker.Name);

            var sceneTopic = new DialogTopic(Context.GetNextFormKey(), Context.Release) {
                Priority = 2500,
                Quest = new FormLinkNullable<IQuestGetter>(quest),
                Category = DialogTopic.CategoryEnum.Scene,
                Subtype = DialogTopic.SubtypeEnum.Scene,
                SubtypeName = "SCEN",
                Responses = topic.TopicInfos
                    .Select(info => GetResponses(quest, info))
                    .ToExtendedList(),
            };
            Context.AddDialogTopic(sceneTopic);

            AddTopic(sceneTopic, aliasSpeaker.AliasIndex);
        }

        scene.LastActionIndex = Convert.ToUInt32(topics.Count * _aliasIndices.Count);

        void AddTopic(IDialogTopicGetter topic, int speakerAliasId) {
            scene.Phases.Add(new ScenePhase { Name = string.Empty, EditorWidth = 200 });

            //Speaker action
            var speakerAction = new SceneAction {
                Type = SceneAction.TypeEnum.Dialog,
                ActorID = speakerAliasId,
                Emotion = Emotion.Neutral,
                EmotionValue = 0,
                Flags = new SceneAction.Flag(),
                StartPhase = currentPhaseIndex,
                EndPhase = currentPhaseIndex,
                Topic = new FormLinkNullable<IDialogTopicGetter>(topic.FormKey),
                LoopingMin = 1,
                LoopingMax = 10,
                Index = scene.LastActionIndex,
                Name = string.Empty,
            };

            // We can only be sure who the speaker should look at when there are only two NPCs involved.
            if (AliasSpeakers.Count == 2)
                speakerAction.HeadtrackActorID = _aliasIndices.Find(aliasIndex => aliasIndex != speakerAliasId);

            scene.Actions.Add(speakerAction);
            scene.LastActionIndex += 1;

            //Head track actions
            foreach (var aliasIndex in _aliasIndices.Where(aliasIndex => aliasIndex != speakerAliasId)) {
                scene.Actions.Add(new SceneAction {
                    Type = SceneAction.TypeEnum.Dialog,
                    ActorID = aliasIndex,
                    Emotion = Emotion.Neutral,
                    EmotionValue = 0,
                    Flags = SceneAction.Flag.FaceTarget,
                    StartPhase = currentPhaseIndex,
                    EndPhase = currentPhaseIndex,
                    Topic = new FormLinkNullable<IDialogTopicGetter>(FormKey.Null),
                    LoopingMin = 1,
                    LoopingMax = 10,
                    HeadtrackActorID = speakerAliasId,
                    Index = scene.LastActionIndex,
                    Name = string.Empty,
                });
                scene.LastActionIndex += 1;
            }

            currentPhaseIndex++;
        }
    }

    protected static QuestAlias GetEventAlias(string name, byte[] eventData, FormKey npc1, FormKey npc2) {
        return new QuestAlias {
            Name = name,
            FindMatchingRefFromEvent = new FindMatchingRefFromEvent {
                FromEvent = RecordTypes.ADIA,
                EventData = eventData,
            },
            Conditions = [
                GetFormKeyCondition(Condition.Function.GetIsID, npc1, 1, true),
                GetFormKeyCondition(Condition.Function.GetIsID, npc2, 1, true),
            ],
            Flags = QuestAlias.Flag.AllowReserved,
            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
        };
    }

    protected static QuestAlias GetAlias(AliasSpeaker aliasSpeaker) {
        return new QuestAlias {
            ID = Convert.ToUInt32(aliasSpeaker.AliasIndex),
            Name = aliasSpeaker.Name,
            UniqueActor = new FormLinkNullable<INpcGetter>(aliasSpeaker.FormKey),
            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
        };
    }

    protected Scene AddScene(string editorId, FormKey questFormKey) {
        _aliasIndices = NameMappedSpeakers.SelectMany(x => x.Speakers.Select(speaker => speaker.AliasIndex)).ToList();

        return new Scene(Context.GetNextFormKey(), Context.Release) {
            EditorID = editorId,
            Actions = new ExtendedList<SceneAction>(),
            Actors = _aliasIndices.Select(id => new SceneActor {
                    BehaviorFlags = SceneActor.BehaviorFlag.DeathEnd | SceneActor.BehaviorFlag.CombatEnd
                                                                     | SceneActor.BehaviorFlag.DialoguePause,
                    Flags = new SceneActor.Flag(),
                    ID = Convert.ToUInt32(id),
                })
                .ToExtendedList(),
            Quest = new FormLinkNullable<IQuestGetter>(questFormKey),
        };
    }

    public override void PreProcess(List<DialogueTopic> topics) {
        AliasSpeakers = GetSpeakers(topics);

        NameMappedSpeakers = AliasSpeakers
            .GroupBy(x => x.FormKey)
            .Select(x => (x.Key, x.ToList()))
            .ToList();

        //break up topics for every new speaker
        var separatedTopics = TransformLines(topics);

        topics.Clear();
        topics.AddRange(separatedTopics);

        PreProcessSpeakers();
    }

    public abstract void PreProcessSpeakers();

    protected virtual IReadOnlyList<AliasSpeaker> GetSpeakers(List<DialogueTopic> topics) {
        //Get speaker strings
        var speakerNames = topics
            .SelectMany(topic => topic.TopicInfos)
            .SelectMany(topicInfo => topicInfo.Responses, (_, response) => SceneLineRegex().Match(response.Response))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToHashSet();

        //Map speaker form keys
        return Context.GetAliasSpeakers(speakerNames);
    }

    protected virtual List<DialogueTopic> TransformLines(List<DialogueTopic> topics) {
        var separatedTopics = new List<DialogueTopic>();
        var currentSpeaker = string.Empty;
        var currentInfos = new List<DialogueTopicInfo>();

        foreach (var topic in topics) {
            foreach (var info in topic.TopicInfos) {
                if (info.Responses.Count != 1) throw new InvalidOperationException("Only one response per topic is allowed");
                
                var response = info.Responses[0];
                var match = SceneLineRegex().Match(response.Response);
                if (!match.Success) continue;

                var speaker = match.Groups[1].Value;
                if (currentSpeaker != speaker || (currentInfos.Count > 0 && currentInfos[^1].SharedInfo is not null)) {
                    AddCurrentTopic();
                    currentSpeaker = speaker;
                }

                info.Responses[0].Response = match.Groups[2].Value;
                info.Speaker = GetSpeaker(currentSpeaker);
                currentInfos.Add(info);
            }

            AddCurrentTopic();
            currentSpeaker = string.Empty;
        }

        if (currentInfos.Count != 0) AddCurrentTopic();
        return separatedTopics;

        void AddCurrentTopic() {
            if (currentInfos.Count != 0) {
                var dialogueTopic = new DialogueTopic {
                    TopicInfos = currentInfos.ToList(),
                };

                separatedTopics.Add(dialogueTopic);
            }

            currentInfos.Clear();
        }
    }

    public override void PostProcess() { }

    public AliasSpeaker GetSpeaker(string name) {
        name = ISpeaker.GetSpeakerName(name);
        foreach (var (_, speakers) in NameMappedSpeakers) {
            foreach (var speaker in speakers) {
                if (speaker.Name == name) return speaker;
            }
        }

        throw new InvalidOperationException($"Didn't find speaker {name}");
    }

    [GeneratedRegex(@"([^:]*):? *([\S\s]+)")]
    private static partial Regex SceneLineRegex();
}
