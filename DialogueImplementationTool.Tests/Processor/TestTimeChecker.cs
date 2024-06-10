using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using FluentAssertions;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
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
        var conditionData1 = topicInfo.ExtraConditions[0].Data.Should().BeOfType<GetGlobalValueConditionData>();
        conditionData1.Subject.Global.Link.FormKey.Should().Be(Skyrim.Global.GameHour.FormKey);

        var condition2 = topicInfo.ExtraConditions[1].Should().BeOfType<ConditionFloat>();
        condition2.Subject.ComparisonValue.Should().Be(12.5f);
        condition2.Subject.CompareOperator.Should().Be(CompareOperator.LessThanOrEqualTo);
        var conditionData2 = topicInfo.ExtraConditions[1].Data.Should().BeOfType<GetGlobalValueConditionData>();
        conditionData2.Subject.Global.Link.FormKey.Should().Be(Skyrim.Global.GameHour.FormKey);
    }
}
