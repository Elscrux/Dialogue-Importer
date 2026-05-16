using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Services;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
namespace DialogueImplementationTool.Tests.Factory;

/// <summary>
/// Tests for CommentSceneFactory covering:
/// - Creation of multiple comment types (Danger, Entrance, View)
/// - Quest creation with proper configuration
/// - Scene creation with proper setup
/// - Story manager node registration
/// </summary>
public sealed class TestCommentSceneFactory {
    private readonly TestConstants _testConstants = new() {
        FormKeySelection = new InjectedFormKeySelection(new Dictionary<string, FormKey> {
            { "Default NPC Voice Types formlist", TestConstants.FormList1FormKey },
            { "Unique NPC Voice Types formlist", TestConstants.FormList2FormKey },
        })
    };

    [Fact]
    public void TestGenerateDialogueMultipleCommentsInSeriesOfIdles() {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        List<DialogueTopic> topics = [
            new() {
                TopicInfos = [
                    new DialogueTopicInfo {
                        Speaker = _testConstants.Speaker1,
                        Responses = [
                            new DialogueResponse {
                                StartNotes = [new Note { Text = "Comment: Danger" }],
                                Response = "Danger!",
                            },
                            new DialogueResponse {
                                StartNotes = [new Note { Text = "Comment: Entrance" }],
                                Response = "Entrance!",
                            },
                            new DialogueResponse {
                                StartNotes = [new Note { Text = "Comment: Danger" }],
                                Response = "Danger2!",
                            },
                            new DialogueResponse {
                                StartNotes = [new Note { Text = "Comment: View" }],
                                Response = "View!",
                            }
                        ],
                    }
                ]
            }
        ];

        // Act
        factory.GenerateDialogue(topics);

        // Assert
        var quests = _testConstants.SkyrimDialogueContext.Mod.EnumerateMajorRecords<Quest>()
            .Except([_testConstants.Quest])
            .ToArray();

        quests.Should().HaveCount(3);

        // Each comment type creates one scene
        _testConstants.SkyrimDialogueContext.Mod.EnumerateMajorRecords<Scene>().Should().HaveCount(3);

        // All quests should be registered under the same story manager node
        _testConstants.SkyrimDialogueContext.Mod.EnumerateMajorRecords<StoryManagerQuestNode>()
            .Should().ContainSingle(n => quests.All(q => n.Quests.Any(qn => qn.Quest.FormKey == q.FormKey)));

        // Dialogue should be implemented
        var dialogTopics = _testConstants.SkyrimDialogueContext.Mod.EnumerateMajorRecords<DialogTopic>().ToArray();
        dialogTopics.Should().HaveCount(3);

        foreach (var dialogTopic in dialogTopics) {
            foreach (var response in dialogTopic.Responses) {
                response.Conditions.Should().ContainSingle(c => c.Data is IGetIsIDConditionDataGetter);
            }
        }
    }

    [Theory]
    [InlineData("Danger")]
    [InlineData("Entrance")]
    [InlineData("View")]
    public void TestCreateFollowerDangerCommentType(string type) {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        var dangerConfig = factory.Configs[type];

        var dialogTopic = new DialogTopic(_testConstants.SkyrimDialogueContext.GetNextFormKey(),
            _testConstants.SkyrimDialogueContext.Release) {
            Quest = new FormLinkNullable<IQuestGetter>(_testConstants.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Scene,
            Subtype = DialogTopic.SubtypeEnum.Scene,
        };

        // Act
        var quest = factory.CreateCommentType(dangerConfig, dialogTopic);

        // Assert
        quest.Should().NotBeNull();
        quest.EditorID.Should().Be(dangerConfig.QuestEditorID);
        quest.Priority.Should().Be(dangerConfig.Priority);
        quest.Filter.Should().Be(dangerConfig.QuestFilterPath);
        quest.Aliases.Should().ContainSingle();
        quest.Aliases[0].Name.Should().Be("Follower");
    }

    [Theory]
    [InlineData("Danger")]
    [InlineData("Entrance")]
    [InlineData("View")]
    public void TestDangerCommentSceneCreation(string type) {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        var dangerConfig = factory.Configs[type];

        var dialogTopic = new DialogTopic(_testConstants.SkyrimDialogueContext.GetNextFormKey(),
            _testConstants.SkyrimDialogueContext.Release) {
            Quest = new FormLinkNullable<IQuestGetter>(_testConstants.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Scene,
            Subtype = DialogTopic.SubtypeEnum.Scene,
        };

        // Act
        var quest = factory.CreateCommentType(dangerConfig, dialogTopic);

        // Assert
        var scenes = _testConstants.Mod.Scenes.Where(s => s.Quest.FormKey == quest.FormKey).ToList();
        scenes.Should().ContainSingle();

        var scene = scenes.First();
        scene.EditorID.Should().Be(quest.EditorID + "Scene");
        scene.Actors.Should().ContainSingle();
        scene.Actions.Should().ContainSingle();
    }

    [Theory]
    [InlineData("Danger")]
    [InlineData("Entrance")]
    [InlineData("View")]
    public void TestCreateSameCommentTypeMultipleTimes(string type) {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        var dangerConfig = factory.Configs[type];

        var dialogTopic1 = new DialogTopic(_testConstants.SkyrimDialogueContext.GetNextFormKey(),
            _testConstants.SkyrimDialogueContext.Release) {
            Quest = new FormLinkNullable<IQuestGetter>(_testConstants.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Scene,
            Subtype = DialogTopic.SubtypeEnum.Scene,
        };

        var dialogTopic2 = new DialogTopic(_testConstants.SkyrimDialogueContext.GetNextFormKey(),
            _testConstants.SkyrimDialogueContext.Release) {
            Quest = new FormLinkNullable<IQuestGetter>(_testConstants.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Scene,
            Subtype = DialogTopic.SubtypeEnum.Scene,
        };

        // Act
        var quest1 = factory.CreateCommentType(dangerConfig, dialogTopic1);
        var quest2 = factory.CreateCommentType(dangerConfig, dialogTopic2);

        // Assert
        quest1.FormKey.Should().Be(quest2.FormKey);
        quest1.EditorID.Should().Be(quest2.EditorID);
    }

    [Fact]
    public void TestCreateAllCommentTypesSequentially() {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        var commentTypes = factory.Configs.ToList();

        var createdQuests = new Dictionary<string, Quest>();

        // Act
        foreach (var (identifier, config) in commentTypes) {
            var dialogTopic = new DialogTopic(_testConstants.SkyrimDialogueContext.GetNextFormKey(),
                _testConstants.SkyrimDialogueContext.Release) {
                Quest = new FormLinkNullable<IQuestGetter>(_testConstants.Quest.FormKey),
                Category = DialogTopic.CategoryEnum.Scene,
                Subtype = DialogTopic.SubtypeEnum.Scene,
            };

            var quest = factory.CreateCommentType(config, dialogTopic);
            createdQuests[identifier] = quest;
        }

        // Assert
        createdQuests.Should().HaveCount(3);
        createdQuests.Keys.Should().ContainInOrder("Danger", "Entrance", "View");
        var formKeys = createdQuests.Values.Select(q => q.FormKey).Distinct().ToList();
        formKeys.Should().HaveCount(3);

        _testConstants.SkyrimDialogueContext.Mod.EnumerateMajorRecords<Scene>()
            .Should().HaveCount(3);

        _testConstants.SkyrimDialogueContext.Mod.EnumerateMajorRecords<StoryManagerQuestNode>()
            .Should().ContainSingle(n => createdQuests.Values.Any(q => n.Quests.Any(qn => qn.Quest.FormKey == q.FormKey)));

        _testConstants.SkyrimDialogueContext.Mod.EnumerateMajorRecords<StoryManagerQuestNode>()
            .Should().ContainSingle();
    }

    [Theory]
    [InlineData("Danger")]
    [InlineData("Entrance")]
    [InlineData("View")]
    public void TestQuestAliasConfiguration(string type) {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        var dangerConfig = factory.Configs[type];

        // Act
        var quest = factory.GetOrCreateQuest(dangerConfig);

        // Assert
        quest.Aliases.Should().ContainSingle();
        var alias = quest.Aliases[0];

        alias.Name.Should().Be("Follower");
        alias.FindMatchingRefFromEvent.Should().NotBeNull();
        alias.FindMatchingRefFromEvent!.FromEvent.Should().BeEquivalentTo(RecordTypes.SCPT);
        alias.Conditions.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Danger")]
    [InlineData("Entrance")]
    [InlineData("View")]
    public void TestSceneActorConfiguration(string type) {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        var dangerConfig = factory.Configs[type];
        var quest = factory.GetOrCreateQuest(dangerConfig);

        var dialogTopic = new DialogTopic(_testConstants.SkyrimDialogueContext.GetNextFormKey(),
            _testConstants.SkyrimDialogueContext.Release) {
            Quest = new FormLinkNullable<IQuestGetter>(_testConstants.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Scene,
            Subtype = DialogTopic.SubtypeEnum.Scene,
        };

        // Act
        var scene = factory.GetOrCreateScene(dialogTopic, quest);

        // Assert
        scene.Flags.Should().HaveFlag(Scene.Flag.BeginOnQuestStart);
        scene.Flags.Should().HaveFlag(Scene.Flag.StopQuestOnEnd);

        scene.Actors.Should().ContainSingle();
        var actor = scene.Actors[0];
        actor.ID.Should().Be(0);
        actor.BehaviorFlags.Should().HaveFlag(SceneActor.BehaviorFlag.DeathEnd);
        actor.BehaviorFlags.Should().HaveFlag(SceneActor.BehaviorFlag.CombatEnd);
        actor.BehaviorFlags.Should().HaveFlag(SceneActor.BehaviorFlag.DialoguePause);
    }

    [Theory]
    [InlineData("Danger")]
    [InlineData("Entrance")]
    [InlineData("View")]
    public void TestSceneActionConfiguration(string type) {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        var dangerConfig = factory.Configs[type];
        var quest = factory.GetOrCreateQuest(dangerConfig);

        var dialogTopicFormKey = _testConstants.SkyrimDialogueContext.GetNextFormKey();
        var dialogTopic = new DialogTopic(dialogTopicFormKey, _testConstants.SkyrimDialogueContext.Release) {
            Quest = new FormLinkNullable<IQuestGetter>(_testConstants.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Scene,
            Subtype = DialogTopic.SubtypeEnum.Scene,
        };

        // Act
        var scene = factory.GetOrCreateScene(dialogTopic, quest);

        // Assert
        scene.Actions.Should().ContainSingle();
        var action = scene.Actions[0];

        action.ActorID.Should().Be(0);
        action.Index.Should().Be(1);
        action.StartPhase.Should().Be(0);
        action.EndPhase.Should().Be(0);
        action.LoopingMin.Should().Be(1);
        action.LoopingMax.Should().Be(10);
        action.Emotion.Should().Be(Emotion.Neutral);
        action.Topic.FormKey.Should().Be(dialogTopicFormKey);
    }

    [Fact]
    public void TestCreateCommentTypeThrowsOnInvalidTypeName() {
        // Arrange
        var factory = new CommentSceneFactory(_testConstants.SkyrimDialogueContext);
        var dialogTopic = new DialogTopic(_testConstants.SkyrimDialogueContext.GetNextFormKey(),
            _testConstants.SkyrimDialogueContext.Release) {
            Quest = new FormLinkNullable<IQuestGetter>(_testConstants.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Scene,
            Subtype = DialogTopic.SubtypeEnum.Scene,
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            factory.CreateCommentType("InvalidType", dialogTopic));
    }
}
