using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public abstract class SceneFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    private List<int> _aliasIndices = [];
    protected IReadOnlyList<AliasSpeaker> AliasSpeakers = [];

    public override IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) {
        // Add scene response processor before note extractor so the speaker name can be trimmed and the line starts with the note
        // NPC A: [some note] some text.
        // => [some note] some text.        | Speaker: NPC A
        // => some text.                    | Speaker: NPC A, Note: [some note]
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
        PreProcess(topics);

        var (scene, quest) = GetCurrentScene();
        if (scene is null || quest is null) return;

        //Add lines
        AddLines(quest.FormKey, scene, topics.ToList());
    }

    protected abstract (Scene? Scene, IQuest? QuestForDialogue) GetCurrentScene();

    private void AddLines(
        FormKey questOwner,
        Scene scene,
        List<DialogueTopic> topics) {
        uint currentPhaseIndex = 0;

        scene.LastActionIndex ??= 1;

        foreach (var topic in topics) {
            var aliasSpeaker = GetSpeaker(topic.TopicInfos[0].Speaker.NameNoSpaces);

            var sceneTopic = new DialogTopic(Context.GetNextFormKey(), Context.Release) {
                Quest = new FormLinkNullable<IQuestGetter>(questOwner),
                Category = DialogTopic.CategoryEnum.Scene,
                Subtype = DialogTopic.SubtypeEnum.Scene,
                SubtypeName = "SCEN",
                Responses = topic.TopicInfos
                    .Select(info => GetResponses(Context.Quest, info))
                    .ToExtendedList(),
            };
            Context.AddRecord(sceneTopic);

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

    protected Scene AddScene(string editorId, FormKey questFormKey) {
        _aliasIndices = AliasSpeakers.Select(speaker => speaker.AliasIndex).ToList();

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

    public virtual void PreProcess(List<DialogueTopic> topics) {
        // Set up speaker
        AliasSpeakers = GetAliasSpeakers(topics);

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
            .Distinct()
            .ToList();

        var allAliasSpeakers = Context.GetAliasSpeakers(speakerNames);

        var nameMappedSpeakers = allAliasSpeakers
            .GroupBy(x => x.FormLink)
            .Select(x => (x.Key, x.ToList()))
            .ToList();

        // Limit alias speakers to one per form key
        foreach (var (_, aliasSpeakers) in nameMappedSpeakers) {
            if (aliasSpeakers.Count == 1) continue;

            foreach (var aliasSpeaker in aliasSpeakers.Skip(1)) {
                var replacementName = aliasSpeakers[0].Name;
                foreach (var topic in topics) {
                    foreach (var topicInfo in topic.TopicInfos) {
                        if (topicInfo.Prompt.FullText == aliasSpeaker.Name) {
                            topicInfo.Prompt.Text = replacementName;
                        }
                    }
                }
            }
        }

        //Map speaker form keys
        return nameMappedSpeakers
            .Select(x => x.Item2[0])
            .ToArray();
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
                            Speaker = new AliasSpeaker(new FormLinkInformation(FormKey.Null, typeof(INpcGetter)), string.Empty),
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
        foreach (var speaker in AliasSpeakers) {
            if (speaker.NameNoSpaces == name) return speaker;
        }

        throw new InvalidOperationException($"Didn't find speaker {name}");
    }
}
