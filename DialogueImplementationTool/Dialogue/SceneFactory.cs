using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public abstract class SceneFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    private List<int> _aliasIndices = [];
    protected IReadOnlyList<AliasSpeaker> AliasSpeakers = [];
    protected List<(FormKey FormKey, List<AliasSpeaker> Speakers)> NameMappedSpeakers = [];

    public override IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) {
        // Add scene response processor
        var noteExtractorIndex = dialogueProcessor.ResponseProcessors.FindIndex(p => p is ResponseNoteExtractor);
        dialogueProcessor.ResponseProcessors.Insert(noteExtractorIndex, new SceneResponseProcessor());

        var processor = base.ConfigureProcessor(dialogueProcessor);
        return new FuncDialogueProcessor(processor) {
            ProcessTopic = (topic, baseAction) => {
                // Convert parsed response format to topic infos
                topic.ConvertResponsesToTopicInfos();
                baseAction(topic);
            },
            ProcessTopics = (topics, baseAction) => {
                var mergedLines = MergeSpeakerLines(topics);
                topics.Clear();
                topics.AddRange(mergedLines);

                baseAction(topics);
            },
        };
    }

    public override void GenerateDialogue(List<DialogueTopic> topics) {
        var scene = GetCurrentScene();
        if (scene is null) return;

        //Add lines
        AddLines(scene, topics.ToList());
    }

    protected abstract Scene? GetCurrentScene();

    private void AddLines(
        Scene scene,
        List<DialogueTopic> topics) {
        uint currentPhaseIndex = 0;

        scene.LastActionIndex ??= 1;

        foreach (var topic in topics) {
            var aliasSpeaker = GetSpeaker(topic.TopicInfos[0].Speaker.NameNoSpaces);

            var sceneTopic = new DialogTopic(Context.GetNextFormKey(), Context.Release) {
                Quest = new FormLinkNullable<IQuestGetter>(Context.Quest),
                Category = DialogTopic.CategoryEnum.Scene,
                Subtype = DialogTopic.SubtypeEnum.Scene,
                SubtypeName = "SCEN",
                Responses = topic.TopicInfos
                    .Select(info => GetResponses(Context.Quest, info))
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
        var data1 = new GetIsIDConditionData();
        var data2 = new GetIsIDConditionData();
        data1.Object.Link.SetTo(npc1);
        data2.Object.Link.SetTo(npc2);
        return new QuestAlias {
            Name = name,
            FindMatchingRefFromEvent = new FindMatchingRefFromEvent {
                FromEvent = RecordTypes.ADIA,
                EventData = eventData,
            },
            Conditions = [
                GetFormKeyCondition(data1, or: true),
                GetFormKeyCondition(data2, or: true),
            ],
            Flags = QuestAlias.Flag.AllowReserved,
            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
        };
    }

    protected static QuestAlias CreateAlias(AliasSpeaker aliasSpeaker) {
        return new QuestAlias {
            ID = Convert.ToUInt32(aliasSpeaker.AliasIndex),
            Name = aliasSpeaker.NameNoSpaces,
            UniqueActor = new FormLinkNullable<INpcGetter>(aliasSpeaker.FormKey),
            VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
        };
    }

    protected Scene AddScene(string editorId, FormKey questFormKey) {
        _aliasIndices = NameMappedSpeakers.SelectMany(x => x.Speakers.Select(speaker => speaker.AliasIndex)).ToList();

        return new Scene(Context.GetNextFormKey(), Context.Release) {
            EditorID = editorId,
            Actions = [],
            Actors = _aliasIndices.Select(id => new SceneActor {
                    BehaviorFlags = SceneActor.BehaviorFlag.DeathEnd
                      | SceneActor.BehaviorFlag.CombatEnd
                      | SceneActor.BehaviorFlag.DialoguePause,
                    Flags = new SceneActor.Flag(),
                    ID = Convert.ToUInt32(id),
                })
                .ToExtendedList(),
            Quest = new FormLinkNullable<IQuestGetter>(questFormKey),
        };
    }

    public override void PreProcess(List<DialogueTopic> topics) {
        // Set up speaker
        AliasSpeakers = GetAliasSpeakers(topics);

        NameMappedSpeakers = AliasSpeakers
            .GroupBy(x => x.FormKey)
            .Select(x => (x.Key, x.ToList()))
            .ToList();

        // Set speaker from prompt
        foreach (var topic in topics) {
            SetSpeakerFromPrompt(topic);
        }

        TransformLines(topics);

        PreProcessSpeakers();
    }

    public abstract void PreProcessSpeakers();

    private IReadOnlyList<AliasSpeaker> GetAliasSpeakers(List<DialogueTopic> topics) {
        //Get speaker strings
        var speakerNames = topics
            .SelectMany(topic => topic.TopicInfos)
            .Select(topicInfo => topicInfo.Prompt.FullText)
            .ToHashSet();

        //Map speaker form keys
        return Context.GetAliasSpeakers(speakerNames);
    }

    protected virtual void TransformLines(List<DialogueTopic> topics) {
        FlattenTopicLinks(topics);
    }

    private static void FlattenTopicLinks(List<DialogueTopic> topics) {
        var counter = 0;
        while (counter < topics.Count) {
            var topic = topics[counter];

            // Move links to the main topic
            foreach (var link in topic.EnumerateLinks(false)) {
                topics.Insert(counter + 1, link);
                counter++;
            }

            // Remove links
            foreach (var topicInfo in topic.TopicInfos) {
                topicInfo.Links.Clear();
            }

            counter++;
        }
    }

    private void SetSpeakerFromPrompt(DialogueTopic topic) {
        foreach (var topicInfo in topic.TopicInfos) {
            var speaker = GetSpeaker(topicInfo.Prompt.FullText);
            topicInfo.Speaker = speaker;
            topicInfo.Prompt = string.Empty;

            // Set all links to the same speaker
            foreach (var link in topicInfo.Links.EnumerateLinks(true)) {
                foreach (var info in link.TopicInfos) {
                    info.Speaker = speaker;
                    info.Prompt = string.Empty;
                }
            }
        }
    }

    /// <summary>
    /// Merges lines that have the same speaker
    /// </summary>
    /// <param name="topics">Topics to merge</param>
    /// <returns>Merged topics</returns>
    /// <exception cref="InvalidOperationException">Only one response per topic is allowed</exception>
    private List<DialogueTopic> MergeSpeakerLines(List<DialogueTopic> topics) {
        var separatedTopics = new List<DialogueTopic>();
        string? currentSpeaker = null;
        var currentLines = new List<DialogueResponse>();

        foreach (var topic in topics) {
            foreach (var topicInfo in topic.TopicInfos) {
                if (topicInfo.Responses.Count != 1)
                    throw new InvalidOperationException("Only one response per topic is allowed");

                // Find speaker name in start note and set prompt to it
                var response = topicInfo.Responses[0];
                var note = response.StartNotes.Find(note =>
                    SceneResponseProcessor.GetSpeaker(note) is not null);
                if (note is null) continue;

                response.StartNotes.Remove(note);
                var speaker = SceneResponseProcessor.GetSpeaker(note)!;
                topicInfo.Prompt = speaker;
                currentSpeaker ??= speaker;

                // If the speaker updated or the last info is shared, add the current topic
                if (currentSpeaker != speaker) {
                    AddCurrentTopic();
                    currentSpeaker = speaker;
                }

                currentLines.Add(response);
            }

            AddCurrentTopic();
            currentSpeaker = null;
        }

        return separatedTopics;

        void AddCurrentTopic() {
            if (currentLines.Count != 0 && currentSpeaker is not null) {
                var dialogueTopic = new DialogueTopic {
                    TopicInfos = [
                        new DialogueTopicInfo {
                            Prompt = currentSpeaker,
                            Responses = currentLines.ToList(),
                        },
                    ],
                };

                separatedTopics.Add(dialogueTopic);
            }

            currentLines.Clear();
        }
    }

    public AliasSpeaker GetSpeaker(string name) {
        name = ISpeaker.GetSpeakerName(name);
        foreach (var (_, speakers) in NameMappedSpeakers) {
            foreach (var speaker in speakers) {
                if (speaker.NameNoSpaces == name) return speaker;
            }
        }

        throw new InvalidOperationException($"Didn't find speaker {name}");
    }
}
