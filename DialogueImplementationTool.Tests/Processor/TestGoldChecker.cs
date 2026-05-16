using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using FluentAssertions;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestGoldChecker {
    [Fact]
    public void ResponseLevelGoldChecks() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Prompt = new DialogueText { Text = "I can give you the coin for 600 gold.", },
                    Responses = [
                        new DialogueResponse {
                            Response = "Always a pleasure.",
                            StartNotes = [new Note { Text = "If player has >= 600 gold" }],
                            EndsNotes = [
                                new Note { Text = "success" },
                                new Note { Text = "600 gold removed" },
                            ],
                        },
                        new DialogueResponse {
                            Response = "Doesn't seem like you have the coin.",
                            StartNotes = [new Note { Text = "failure" }],
                            EndsNotes = [new Note { Text = "If player has < 600 gold" }],
                        },
                    ],
                },
            ],
        };

        // Act
        var checker = new GoldChecker();
        checker.Process(topic);

        // Assert
        topic.TopicInfos.Should().HaveCount(2);

        var firstInfo = topic.TopicInfos[0];
        firstInfo.Responses.Should().ContainSingle();
        firstInfo.Responses[0].Response.Should().Be("Always a pleasure.");
        firstInfo.ExtraConditions.Should().ContainSingle();
        var first = firstInfo.ExtraConditions[0].Should().BeOfType<ConditionFloat>().Subject;
        first.CompareOperator.Should().Be(CompareOperator.GreaterThanOrEqualTo);
        first.ComparisonValue.Should().Be(600);
        first.Data.Should().BeOfType<GetItemCountConditionData>();
        firstInfo.Script.EndScriptLines.Should().ContainSingle("Game.GetPlayer().RemoveItem(Gold, 600)");
        firstInfo.Script.Properties.Should().ContainSingle(p => p.ScriptProperty.Name == "Gold");
        firstInfo.Responses[0].Notes().Should().BeEmpty();

        var secondInfo = topic.TopicInfos[1];
        secondInfo.Responses.Should().ContainSingle();
        secondInfo.Responses[0].Response.Should().Be("Doesn't seem like you have the coin.");
        secondInfo.ExtraConditions.Should().BeEmpty();
        secondInfo.Responses[0].Notes().Should().BeEmpty();
    }

    [Fact]
    public void ResponseLevelGoldChecks_MultiLineBranchesStayGrouped() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Prompt = new DialogueText { Text = "I can give you the coin for 600 gold.", },
                    Responses = [
                        new DialogueResponse {
                            Response = "Always a pleasure.",
                            StartNotes = [new Note { Text = "If player has >= 600 gold" }],
                            EndsNotes = [
                                new Note { Text = "success" },
                                new Note { Text = "600 gold removed" },
                            ],
                        },
                        new DialogueResponse {
                            Response = "Here you go.",
                        },
                        new DialogueResponse {
                            Response = "Here you go.",
                        },
                        new DialogueResponse {
                            Response = "Doesn't seem like you have the coin.",
                            StartNotes = [
                                new Note { Text = "If player has < 600 gold" },
                                new Note { Text = "If player has < 600 gold" },
                                new Note { Text = "If player has < 600 gold" },
                            ],
                        },
                        new DialogueResponse {
                            Response = "Come back when you have it.",
                            StartNotes = [new Note { Text = "failure" }],
                        },
                    ],
                },
            ],
        };

        // Act
        var checker = new GoldChecker();
        checker.Process(topic);

        // Assert
        topic.TopicInfos.Should().HaveCount(2);

        var firstInfo = topic.TopicInfos[0];
        firstInfo.Responses.Should().HaveCount(3);
        firstInfo.Responses[0].Response.Should().Be("Always a pleasure.");
        firstInfo.Responses[1].Response.Should().Be("Here you go.");
        firstInfo.ExtraConditions.Should().ContainSingle();
        var first = firstInfo.ExtraConditions[0].Should().BeOfType<ConditionFloat>().Subject;
        first.CompareOperator.Should().Be(CompareOperator.GreaterThanOrEqualTo);
        first.ComparisonValue.Should().Be(600);
        first.Data.Should().BeOfType<GetItemCountConditionData>();
        firstInfo.Script.EndScriptLines.Should().ContainSingle("Game.GetPlayer().RemoveItem(Gold, 600)");
        firstInfo.Script.Properties.Should().ContainSingle(p => p.ScriptProperty.Name == "Gold");
        firstInfo.Responses[0].Notes().Should().BeEmpty();
        firstInfo.Responses[1].Notes().Should().BeEmpty();
        firstInfo.Responses[2].Notes().Should().BeEmpty();

        var secondInfo = topic.TopicInfos[1];
        secondInfo.Responses.Should().HaveCount(2);
        secondInfo.Responses[0].Response.Should().Be("Doesn't seem like you have the coin.");
        secondInfo.Responses[1].Response.Should().Be("Come back when you have it.");
        secondInfo.ExtraConditions.Should().BeEmpty();
        secondInfo.Responses[0].Notes().Should().BeEmpty();
        secondInfo.Responses[1].Notes().Should().BeEmpty();
    }

    [Fact]
    public void ResponseLevelOnlyScripts_AreNotSplit() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Prompt = new DialogueText { Text = "Hello", },
                    Responses = [
                        new DialogueResponse {
                            Response = "Take this",
                            EndsNotes = [new Note { Text = "10 gold added" }],
                        },
                        new DialogueResponse {
                            Response = "Keep it",
                            EndsNotes = [new Note { Text = "5 gold removed" }],
                        },
                    ],
                },
            ],
        };

        // Act
        var checker = new GoldChecker();
        checker.Process(topic);

        // Assert
        topic.TopicInfos.Should().ContainSingle();
        var info = topic.TopicInfos[0];
        info.ExtraConditions.Should().BeEmpty();
        info.Script.EndScriptLines.Should().HaveCount(2);
        info.Script.EndScriptLines.Should().Contain("Game.GetPlayer().AddItem(Gold, 10)");
        info.Script.EndScriptLines.Should().Contain("Game.GetPlayer().RemoveItem(Gold, 5)");
        info.Script.Properties.Should().ContainSingle(p => p.ScriptProperty.Name == "Gold");
    }

    [Fact]
    public void PromptLevelGoldCheck() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Prompt = new DialogueText {
                        Text = "The best I can offer is 400. (Persuade)",
                        StartNotes = [
                            new Note { Text = "if PC has >= 400 gold" },
                            new Note { Text = "hard" },
                        ],
                    },
                    Responses = [
                        new DialogueResponse {
                            Response = "(sigh) Aye, four hundred it is then.",
                            StartNotes = [new Note { Text = "success" }],
                            EndsNotes = [new Note { Text = "400 gold removed" }],
                        },
                        new DialogueResponse {
                            Response = "Six hundred. Take it or leave it.",
                            StartNotes = [new Note { Text = "Failure" }],
                        },
                    ],
                },
            ],
        };

        // Act
        var checker = new GoldChecker();
        checker.Process(topic);

        // Assert
        topic.TopicInfos.Should().HaveCount(2);

        topic.TopicInfos[0].Responses.Should().ContainSingle();
        topic.TopicInfos[0].Responses[0].Response.Should().Be("(sigh) Aye, four hundred it is then.");
        topic.TopicInfos[0].ExtraConditions.Should().ContainSingle();
        var firstCondition = topic.TopicInfos[0].ExtraConditions[0].Should().BeOfType<ConditionFloat>().Subject;
        firstCondition.CompareOperator.Should().Be(CompareOperator.GreaterThanOrEqualTo);
        firstCondition.ComparisonValue.Should().Be(400);
        firstCondition.Data.Should().BeOfType<GetItemCountConditionData>();
        topic.TopicInfos[0].Prompt.Notes().Select(x => x.Text).Should().BeEquivalentTo("hard");
        topic.TopicInfos[0].Responses[0].Notes().Should().BeEmpty();
        topic.TopicInfos[0].Script.EndScriptLines.Should().ContainSingle("Game.GetPlayer().RemoveItem(Gold, 400)");
        topic.TopicInfos[0].Script.Properties.Should().ContainSingle(p => p.ScriptProperty.Name == "Gold");

        topic.TopicInfos[1].Responses.Should().ContainSingle();
        topic.TopicInfos[1].Responses[0].Response.Should().Be("Six hundred. Take it or leave it.");
        topic.TopicInfos[1].ExtraConditions.Should().BeEmpty();
        topic.TopicInfos[1].Prompt.Notes().Select(x => x.Text).Should().BeEquivalentTo("hard");
        topic.TopicInfos[1].Responses[0].Notes().Should().BeEmpty();
    }

    [Fact]
    public void SkillCheckOnlyTopic_IsLeftUntouched() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Prompt = new DialogueText {
                        Text = "The best I can do is 400. (Persuade)",
                        StartNotes = [new Note { Text = "hard" }],
                    },
                    Responses = [
                        new DialogueResponse {
                            Response = "Aye, four hundred it is then.",
                            StartNotes = [new Note { Text = "success" }],
                        },
                        new DialogueResponse {
                            Response = "Take it or leave it.",
                            StartNotes = [new Note { Text = "failure" }],
                        },
                    ],
                },
            ],
        };

        // Act
        var checker = new GoldChecker();
        checker.Process(topic);

        // Assert
        topic.TopicInfos.Should().ContainSingle();
        topic.TopicInfos[0].Prompt.Text.Should().Be("The best I can do is 400. (Persuade)");
        topic.TopicInfos[0].Prompt.Notes().Select(x => x.Text).Should().BeEquivalentTo("hard");
        topic.TopicInfos[0].Responses.Should().HaveCount(2);
        topic.TopicInfos[0].Responses[0].Notes().Count.Should().Be(0);
        topic.TopicInfos[0].Responses[1].Notes().Count.Should().Be(0);
        topic.TopicInfos[0].ExtraConditions.Should().BeEmpty();
        topic.TopicInfos[0].Script.EndScriptLines.Should().BeEmpty();
        topic.TopicInfos[0].Script.Properties.Should().BeEmpty();
    }

    [Fact]
    public void PromptLevelOnlyGoldCondition() {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Prompt = new DialogueText {
                        Text = "Offer",
                        StartNotes = [
                            new Note { Text = "if PC has > 400 gold" },
                            new Note { Text = "hard" },
                        ],
                    },
                    Responses = [
                        new DialogueResponse {
                            Response = "Agree",
                            StartNotes = [new Note { Text = "success" }],
                        },
                        new DialogueResponse {
                            Response = "Decline",
                            StartNotes = [new Note { Text = "failure" }],
                        },
                    ],
                },
            ],
        };

        // Act
        var checker = new GoldChecker();
        checker.Process(topic);

        // Assert
        topic.TopicInfos.Should().HaveCount(2);

        var first = topic.TopicInfos[0];
        first.Responses.Should().ContainSingle();
        first.ExtraConditions.Should().ContainSingle();
        var cond = first.ExtraConditions[0].Should().BeOfType<ConditionFloat>().Subject;
        cond.CompareOperator.Should().Be(CompareOperator.GreaterThan);
        cond.ComparisonValue.Should().Be(400);
        cond.Data.Should().BeOfType<GetItemCountConditionData>();
        first.Script.EndScriptLines.Should().BeEmpty();
        first.Script.Properties.Should().BeEmpty();
        first.Prompt.Notes().Select(x => x.Text).Should().BeEquivalentTo("hard");
        first.Responses[0].Notes().Should().BeEmpty();

        var second = topic.TopicInfos[1];
        second.ExtraConditions.Should().BeEmpty();
        second.Prompt.Notes().Select(x => x.Text).Should().BeEquivalentTo("hard");
        second.Responses[0].Notes().Should().BeEmpty();
    }

    [Theory]
    [InlineData(">", CompareOperator.GreaterThan)]
    [InlineData(">=", CompareOperator.GreaterThanOrEqualTo)]
    [InlineData("<", CompareOperator.LessThan)]
    [InlineData("<=", CompareOperator.LessThanOrEqualTo)]
    [InlineData("=", CompareOperator.EqualTo)]
    public void ComparisonOperatorVariants_AreParsedCorrectly(string compare, CompareOperator expectedOperator) {
        // Arrange
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Prompt = new DialogueText {
                        Text = "Offer",
                        StartNotes = [new Note { Text = $"if PC has {compare} 125 gold" }],
                    },
                    Responses = [new DialogueResponse { Response = "Ok" }],
                },
            ],
        };

        // Act
        var checker = new GoldChecker();
        checker.Process(topic);

        // Assert
        topic.TopicInfos.Should().ContainSingle();
        topic.TopicInfos[0].ExtraConditions.Should().ContainSingle();
        var condition = topic.TopicInfos[0].ExtraConditions[0].Should().BeOfType<ConditionFloat>().Subject;
        condition.CompareOperator.Should().Be(expectedOperator);
        condition.ComparisonValue.Should().Be(125);
        condition.Data.Should().BeOfType<GetItemCountConditionData>();
        topic.TopicInfos[0].Prompt.Notes().Should().BeEmpty();
        topic.TopicInfos[0].Responses[0].Notes().Should().BeEmpty();
    }
}
