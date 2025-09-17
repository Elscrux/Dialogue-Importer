using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Services;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestDeadAliveChecker {
    private readonly TestConstants _testConstants = new() {
        FormKeySelection = new InjectedFormKeySelection(new Dictionary<string, FormKey> {
            { "Select: X", TestConstants.Speaker1FormKey }
        })
    };

    private static void CheckDeadAliveTopicInfo(DialogueTopicInfo topicInfo, params IReadOnlyList<string> responses) {
        topicInfo.Responses.Should().HaveCount(responses.Count);
        topicInfo.Responses.Select(x => x.Response).Should().BeEquivalentTo(responses);
        topicInfo.AllNotes().Should().BeEmpty();
        topicInfo.ExtraConditions.Should().ContainSingle();
        topicInfo.ExtraConditions[0].Data.Should().BeOfType<GetDeadCountConditionData>();
    }

    private static void CheckOtherTopicInfo(DialogueTopicInfo topicInfo, string response) {
        topicInfo.Responses.Should().ContainSingle();
        topicInfo.Responses[0].Response.Should().Be(response);
        topicInfo.AllNotes().Should().BeEmpty();
        topicInfo.ExtraConditions.Should().BeEmpty();
    }

    [Fact]
    public void TestFirst() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = _testConstants.Speaker1,
                    Responses = [
                        new DialogueResponse { Response = "A", StartNotes = [new Note { Text = "X is alive" }] },
                        new DialogueResponse { Response = "B" },
                    ],
                },
            ],
        };

        // Act
        var deadAliveChecker = new DeadAliveChecker(_testConstants.SkyrimDialogueContext);
        deadAliveChecker.Process(topic);

        // Assert
        topic.TopicInfos.Should().HaveCount(2);

        CheckDeadAliveTopicInfo(topic.TopicInfos[0], "A");
        topic.TopicInfos[0].ExtraConditions[0].CompareOperator.Should().Be(CompareOperator.EqualTo);

        CheckOtherTopicInfo(topic.TopicInfos[1], "");

        topic.TopicInfos[0].Links.Should().ContainSingle();
        topic.TopicInfos[0].InvisibleContinue.Should().BeTrue();
        topic.TopicInfos[1].Links.Should().ContainSingle();
        topic.TopicInfos[1].InvisibleContinue.Should().BeTrue();
        topic.TopicInfos[0].Links[0].Should().BeSameAs(topic.TopicInfos[1].Links[0]);

        topic.TopicInfos[0].Links[0].TopicInfos.Should().ContainSingle();
        var linkedTopicInfo = topic.TopicInfos[0].Links[0].TopicInfos[0];
        CheckOtherTopicInfo(linkedTopicInfo, "B");

        linkedTopicInfo.Links.Should().BeEmpty();
        linkedTopicInfo.InvisibleContinue.Should().BeFalse();
    }

    [Fact]
    public void TestSecond() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = _testConstants.Speaker1,
                    Responses = [
                        new DialogueResponse { Response = "A" },
                        new DialogueResponse { Response = "B", StartNotes = [new Note { Text = "X is alive" }] },
                    ],
                },
            ],
        };

        // Act
        var deadAliveChecker = new DeadAliveChecker(_testConstants.SkyrimDialogueContext);
        deadAliveChecker.Process(topic);

        // Assert
        topic.TopicInfos.Should().ContainSingle();

        CheckOtherTopicInfo(topic.TopicInfos[0], "A");

        topic.TopicInfos[0].Links.Should().ContainSingle();
        topic.TopicInfos[0].InvisibleContinue.Should().BeTrue();
        var links = topic.TopicInfos[0].Links[0].TopicInfos;
        links.Should().ContainSingle();

        CheckDeadAliveTopicInfo(links[0], "B");
        links[0].ExtraConditions[0].CompareOperator.Should().Be(CompareOperator.EqualTo);
    }

    [Fact]
    public void TestEitherOr() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = _testConstants.Speaker1,
                    Responses = [
                        new DialogueResponse { Response = "A", StartNotes = [new Note { Text = "X is alive" }] },
                        new DialogueResponse { Response = "B", StartNotes = [new Note { Text = "X is dead" }] },
                    ],
                },
            ],
        };

        // Act
        var deadAliveChecker = new DeadAliveChecker(_testConstants.SkyrimDialogueContext);
        deadAliveChecker.Process(topic);

        // Assert
        topic.TopicInfos.Should().HaveCount(2);

        CheckDeadAliveTopicInfo(topic.TopicInfos[0], "A");
        topic.TopicInfos[0].Links.Should().BeEmpty();
        topic.TopicInfos[0].InvisibleContinue.Should().BeFalse();
        topic.TopicInfos[0].ExtraConditions[0].CompareOperator.Should().Be(CompareOperator.EqualTo);

        CheckOtherTopicInfo(topic.TopicInfos[1], "B");
        topic.TopicInfos[1].Links.Should().BeEmpty();
        topic.TopicInfos[1].InvisibleContinue.Should().BeFalse();
    }

    [Fact]
    public void TestComplex() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = _testConstants.Speaker1,
                    Responses = [
                        new DialogueResponse { Response = "A" },
                        new DialogueResponse { Response = "B", StartNotes = [new Note { Text = "X is alive" }] },
                        new DialogueResponse { Response = "C", StartNotes = [new Note { Text = "X is alive" }] },
                        new DialogueResponse { Response = "D", StartNotes = [new Note { Text = "X is dead" }] },
                        new DialogueResponse { Response = "E" },
                    ],
                },
            ],
        };

        // Act
        var deadAliveChecker = new DeadAliveChecker(_testConstants.SkyrimDialogueContext);
        deadAliveChecker.Process(topic);

        // Assert
        topic.TopicInfos.Should().ContainSingle();
        CheckOtherTopicInfo(topic.TopicInfos[0], "A");

        topic.TopicInfos[0].Links.Should().ContainSingle();
        topic.TopicInfos[0].InvisibleContinue.Should().BeTrue();
        var links = topic.TopicInfos[0].Links[0].TopicInfos;
        links.Should().HaveCount(2);

        CheckDeadAliveTopicInfo(links[0], "B", "C");
        links[0].ExtraConditions[0].CompareOperator.Should().Be(CompareOperator.EqualTo);

        CheckOtherTopicInfo(links[1], "D");

        links[0].Links.Should().ContainSingle();
        links[0].InvisibleContinue.Should().BeTrue();
        links[1].Links.Should().ContainSingle();
        links[1].InvisibleContinue.Should().BeTrue();
        links[0].Links[0].Should().BeSameAs(links[1].Links[0]);

        links[0].Links[0].TopicInfos.Should().ContainSingle();
        CheckOtherTopicInfo(links[0].Links[0].TopicInfos[0], "E");

        links[0].Links[0].TopicInfos[0].Links.Should().BeEmpty();
        links[0].Links[0].TopicInfos[0].InvisibleContinue.Should().BeFalse();
    }
}
