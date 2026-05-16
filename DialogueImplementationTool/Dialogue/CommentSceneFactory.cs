using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

/// <summary>
/// Factory for creating comment scenes for dialogue topics marked with "Comment: TypeName" notes.
/// Uses the CommentSystemManager to handle the creation of quests, scenes, and story manager nodes.
/// </summary>
public sealed partial class CommentSceneFactory(IDialogueContext context) : OneLinerFactory(context) {
    [GeneratedRegex("Comment: (.+)", RegexOptions.IgnoreCase)]
    private static partial Regex CommentRegex { get; }

    [GeneratedRegex("(.*\b)?Danger", RegexOptions.IgnoreCase)]
    private static partial Regex DangerRegex { get; }

    [GeneratedRegex("(.*\b)?Entrance", RegexOptions.IgnoreCase)]
    private static partial Regex EntranceRegex { get; }

    [GeneratedRegex("(.*\b)?View", RegexOptions.IgnoreCase)]
    private static partial Regex ViewRegex { get; }

    public override IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) {
        dialogueProcessor.TopicInfoProcessors.Add(new RandomTopicInfo());
        return base.ConfigureProcessor(dialogueProcessor);
    }

    public override void GenerateDialogue(List<DialogueTopic> topics) {
        var groupedResponses = GetCommentType(topics)
            .GroupBy(x => x.CommentType)
            .ToDictionary(
                x => x.Key,
                x => x
                    .GroupBy(r => r.Speaker)
                    .ToDictionary(g => g.Key, g => g.Select(y => y.Response).ToList()));

        foreach (var (commentType, speakerResponses) in groupedResponses) {
            // Create the comment records (quest, scene, story manager node)
            var dialogTopic = new DialogTopic(Context.GetNextFormKey(), Context.Release) {
                Quest = new FormLinkNullable<IQuestGetter>(Context.Quest.FormKey),
                Category = DialogTopic.CategoryEnum.Scene,
                Subtype = DialogTopic.SubtypeEnum.Scene,
                SubtypeName = "SCEN",
            };

            var quest = CreateCommentType(commentType, dialogTopic);

            foreach (var (speaker, responses) in speakerResponses) {
                var topic = new DialogueTopic {
                    TopicInfos = [
                        new DialogueTopicInfo {
                            Speaker = speaker,
                            Responses = responses.ToExtendedList(),
                        }
                    ],
                };

                // Generate dialogue from topics
                GenerateDialogue(quest, [topic], dialogTopic);
            }
        }
    }

    private static IEnumerable<(string CommentType, DialogueResponse Response, ISpeaker Speaker)> GetCommentType(
        List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            foreach (var topicInfo in topic.TopicInfos) {
                foreach (var response in topicInfo.Responses) {
                    foreach (var note in response.Notes()) {
                        var match = CommentRegex.Match(note.Text);
                        if (!match.Success) continue;

                        response.RemoveNote(note);
                        yield return (match.Groups[1].Value, response, topicInfo.Speaker);
                    }
                }
            }
        }
    }

    public readonly IReadOnlyDictionary<string, CommentTypeConfiguration> Configs =
        new Dictionary<string, CommentTypeConfiguration> {
            { "Danger", GetFollowerCommentConfig(context, "Danger", DangerRegex, Skyrim.Keyword.WICommentDanger) },
            { "Entrance", GetFollowerCommentConfig(context, "Entrance", EntranceRegex, Skyrim.Keyword.WICommentEntrances) },
            { "View", GetFollowerCommentConfig(context, "View", ViewRegex, Skyrim.Keyword.WICommentSetPiece) },
        };

    private static CommentTypeConfiguration GetFollowerCommentConfig(
        IDialogueContext context,
        string type,
        Regex regex,
        FormLink<IKeywordGetter> keyword) {
        var defaultNpcVoiceTypeList = context.SelectRecord<FormList, IFormListGetter>("Default NPC Voice Types formlist");
        var uniqueNpcVoiceTypeList = context.SelectRecord<FormList, IFormListGetter>("Unique NPC Voice Types formlist");

        return new CommentTypeConfiguration {
            TypeRegex = regex,
            QuestEditorID = context.Prefix + $"FollowerComment{type}",
            StoryManagerQuestNodeEditorID = context.Prefix + "WIFollowerCommentNode",
            DisplayName = $"Follower {type} Comment",
            ExtraDialogueConditions = [
                new GetInFactionConditionData {
                    Faction = { Link = { FormKey = Skyrim.Faction.PotentialFollowerFaction.FormKey } },
                }.ToConditionFloat(),
                new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = defaultNpcVoiceTypeList.FormKey } },
                }.ToConditionFloat(or: true),
                new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = uniqueNpcVoiceTypeList.FormKey } },
                }.ToConditionFloat()
            ],
            ExtraEventConditions = [
                new GetEventDataConditionData {
                    Function = GetEventDataConditionData.EventFunction.GetIsID,
                    Member = GetEventDataConditionData.EventMember.Keyword,
                    Record = new FormLink<ISkyrimMajorRecordGetter>(keyword.FormKey),
                }.ToConditionFloat(),
                new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = defaultNpcVoiceTypeList.FormKey } },
                    RunOnType = Condition.RunOnType.EventData,
                    RunOnTypeIndex = 12626, // Actor 1
                }.ToConditionFloat(or: true),
                new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = uniqueNpcVoiceTypeList.FormKey } },
                    RunOnType = Condition.RunOnType.EventData,
                    RunOnTypeIndex = 12626, // Actor 1
                }.ToConditionFloat(),
            ],
        };
    }

    /// <summary>
    /// Creates quest, scene, and story manager node for a specific comment type
    /// </summary>
    /// <param name="typeName">The name of the comment type to create</param>
    /// <param name="dialogTopic">The dialogue topic for the scene</param>
    /// <returns>Created records: quest, scene, and optional story manager node</returns>
    /// <exception cref="InvalidOperationException">Thrown if comment type not found</exception>
    public Quest CreateCommentType(string typeName, IDialogTopicGetter dialogTopic) {
        var config = Configs.Values.FirstOrDefault(c => c.TypeRegex.IsMatch(typeName));
        if (config == null) {
            throw new InvalidOperationException($"Comment type '{typeName}' is not registered");
        }

        return CreateCommentType(config, dialogTopic);
    }

    /// <summary>
    /// Creates quest, scene, and story manager node for a specific comment configuration
    /// </summary>
    public Quest CreateCommentType(CommentTypeConfiguration config, IDialogTopicGetter dialogTopic) {

        // Create quest
        var quest = GetOrCreateQuest(config);

        // Create scene
        GetOrCreateScene(dialogTopic, quest);

        // Create/get story manager node if configured and add quest if not already added
        var storyManagerNode = GetOrCreateStoryManagerNode(config);
        if (storyManagerNode.Quests.All(q => q.Quest.FormKey != quest.FormKey)) {
            storyManagerNode.Quests.Add(new StoryManagerQuest {
                Quest = new FormLinkNullable<IQuestGetter>(quest.FormKey),
            });
        }

        return quest;
    }

    public Quest GetOrCreateQuest(CommentTypeConfiguration config) {
        return context.GetOrAddRecord<Quest, IQuestGetter>(config.QuestEditorID, BuildQuest);

        Quest BuildQuest() {
            var quest = new Quest(context.GetNextFormKey(), context.Release) {
                EditorID = config.QuestEditorID,
                Priority = config.Priority,
                Filter = config.QuestFilterPath,
                DialogConditions = config.ExtraDialogueConditions.ToExtendedList(),
                EventConditions = config.ExtraEventConditions.ToExtendedList(),
                Name = config.DisplayName,
                Event = RecordTypes.SCPT,
                Aliases = [
                    new QuestAlias {
                        Name = "Follower",
                        Flags = QuestAlias.Flag.AllowReserved,
                        FindMatchingRefFromEvent = new FindMatchingRefFromEvent {
                            FromEvent = RecordTypes.SCPT,
                            EventData = (byte[]) [0x52, 0x31, 0x0, 0x0], // Actor 1 from event
                        },
                        Conditions = config.ExtraDialogueConditions.ToExtendedList(),
                    },
                ],
                NextAliasID = 1,
            };

            return quest;
        }
    }

    public Scene GetOrCreateScene(IDialogTopicGetter topic, IQuestGetter quest) {
        var existingScenes = context.Environment.LinkCache.WinningOverrides<ISceneGetter>()
            .Where(s => s.Quest.FormKey == quest.FormKey)
            .ToArray();

        if (existingScenes is [var existingScene]) {
            return context.GetOrAddOverride<Scene, ISceneGetter>(existingScene);
        }

        var sceneEditorId = quest.EditorID + "Scene";
        return context.GetOrAddRecord<Scene, ISceneGetter>(sceneEditorId, BuildScene);

        Scene BuildScene() {
            var scene = new Scene(context.GetNextFormKey(), context.Release) {
                EditorID = sceneEditorId,
                Flags = Scene.Flag.BeginOnQuestStart | Scene.Flag.StopQuestOnEnd | (Scene.Flag) 4,
                Phases = [
                    new ScenePhase {
                        Name = string.Empty,
                        EditorWidth = 200,
                    }
                ],
                Actors = [
                    new SceneActor {
                        ID = 0,
                        BehaviorFlags = SceneActor.BehaviorFlag.DeathEnd
                          | SceneActor.BehaviorFlag.CombatEnd
                          | SceneActor.BehaviorFlag.DialoguePause,
                    }
                ],
                Actions = [
                    new SceneAction {
                        ActorID = 0,
                        Index = 1,
                        StartPhase = 0,
                        EndPhase = 0,
                        Topic = new FormLinkNullable<IDialogTopicGetter>(topic),
                        LoopingMax = 10,
                        LoopingMin = 1,
                        Emotion = Emotion.Neutral,
                        EmotionValue = 0,
                    },
                ],
                Quest = new FormLinkNullable<IQuestGetter>(quest),
                LastActionIndex = 1,
            };

            return scene;
        }
    }

    public StoryManagerQuestNode GetOrCreateStoryManagerNode(CommentTypeConfiguration config) {
        var storyManagerQuestNodeEditorID = context.Prefix + config.StoryManagerQuestNodeEditorID;

        return context.GetOrAddRecord<StoryManagerQuestNode, IStoryManagerQuestNodeGetter>(
            storyManagerQuestNodeEditorID,
            BuildStoryManagerNode);

        StoryManagerQuestNode BuildStoryManagerNode() {
            return new StoryManagerQuestNode(context.GetNextFormKey(), context.Release) {
                EditorID = storyManagerQuestNodeEditorID,
                Parent = new FormLinkNullable<IAStoryManagerNodeGetter>(config.ParentNodeFormKey),
            };
        }
    }
}

/// <summary>
/// Configuration for a single comment type (e.g., Follower Danger, Follower Entrance).
/// This defines how a comment type's quest and scene should be created.
/// </summary>
public class CommentTypeConfiguration {
    /// <summary>
    /// Regex to identify this comment type (e.g., "Danger", "Entrance", "View")
    /// </summary>
    public required Regex TypeRegex { get; set; }

    /// <summary>
    /// EditorID for the comment quest (e.g., "FollowerCommentDanger").
    /// </summary>
    public required string QuestEditorID { get; init; }

    /// <summary>
    /// EditorID for the comment story manager quest (e.g., "WIFollowerCommentNode").
    /// </summary>
    public required string StoryManagerQuestNodeEditorID { get; init; }

    /// <summary>
    /// Human-readable display name (e.g., "Follower Danger Comment")
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Quest priority (typically 40 for comment quests)
    /// </summary>
    public byte Priority { get; set; } = 40;

    /// <summary>
    /// File path for the quest in the quest filter (e.g., "Generic\Dialogue\")
    /// </summary>
    public string QuestFilterPath { get; set; } = @"Generic\Dialogue\";

    /// <summary>
    /// Additional conditions to add to dialogue conditions of the quest and the alias.
    /// </summary>
    public IEnumerable<Condition> ExtraDialogueConditions { get; set; } = [];

    /// <summary>
    /// Additional conditions to add to story manager event conditions of the quest and the alias.
    /// </summary>
    public IEnumerable<Condition> ExtraEventConditions { get; set; } = [];

    /// <summary>
    /// FormKey of the parent story manager node
    /// By default this is set to the root script event node
    /// </summary>
    public FormKey ParentNodeFormKey { get; set; } = FormKey.Factory("01379A:Skyrim.esm");
}
