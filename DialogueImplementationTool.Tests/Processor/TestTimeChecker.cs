﻿using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using FluentAssertions;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestTimeChecker {
    [Fact]
    public void TestProcess() {
        // Arrange
        var topicInfo = new DialogueTopicInfo {
            Responses = [
                new DialogueResponse { StartNotes = [new Note { Text = "10:00 - 12:30" }] },
            ],
        };

        // Act
        var timeChecker = new TimeChecker();
        timeChecker.Process(topicInfo);

        // Assert
        topicInfo.Responses.Should().ContainSingle();
        topicInfo.ExtraConditions.Should().HaveCount(2);

        var condition1 = topicInfo.ExtraConditions[0].Should().BeOfType<ConditionFloat>();
        condition1.Subject.ComparisonValue.Should().Be(10);
        condition1.Subject.CompareOperator.Should().Be(CompareOperator.GreaterThanOrEqualTo);
        condition1.Subject.Flags.Should().NotHaveFlag(Condition.Flag.OR);
        topicInfo.ExtraConditions[0].Data.Should().BeOfType<GetCurrentTimeConditionData>();

        var condition2 = topicInfo.ExtraConditions[1].Should().BeOfType<ConditionFloat>();
        condition2.Subject.ComparisonValue.Should().Be(12.5f);
        condition2.Subject.CompareOperator.Should().Be(CompareOperator.LessThanOrEqualTo);
        condition2.Subject.Flags.Should().NotHaveFlag(Condition.Flag.OR);
        topicInfo.ExtraConditions[1].Data.Should().BeOfType<GetCurrentTimeConditionData>();
    }

    [Fact]
    public void TestWrappingAroundMidnight() {
        // Arrange
        var topicInfo = new DialogueTopicInfo {
            Responses = [
                new DialogueResponse { StartNotes = [new Note { Text = "22:30 - 02:30" }] },
            ],
        };

        // Act
        var timeChecker = new TimeChecker();
        timeChecker.Process(topicInfo);

        // Assert
        topicInfo.Responses.Should().ContainSingle();
        topicInfo.ExtraConditions.Should().HaveCount(2);

        var condition1 = topicInfo.ExtraConditions[0].Should().BeOfType<ConditionFloat>();
        condition1.Subject.ComparisonValue.Should().Be(22.5f);
        condition1.Subject.CompareOperator.Should().Be(CompareOperator.GreaterThanOrEqualTo);
        condition1.Subject.Flags.Should().HaveFlag(Condition.Flag.OR);
        topicInfo.ExtraConditions[0].Data.Should().BeOfType<GetCurrentTimeConditionData>();

        var condition2 = topicInfo.ExtraConditions[1].Should().BeOfType<ConditionFloat>();
        condition2.Subject.ComparisonValue.Should().Be(2.5f);
        condition2.Subject.CompareOperator.Should().Be(CompareOperator.LessThanOrEqualTo);
        condition2.Subject.Flags.Should().NotHaveFlag(Condition.Flag.OR);
        topicInfo.ExtraConditions[1].Data.Should().BeOfType<GetCurrentTimeConditionData>();
    }
}
