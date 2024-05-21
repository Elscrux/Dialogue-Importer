using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Processor;

public class TestPlayerIsRaceChecker {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestDialogueTopicCraneShore1() {
        // Import
        var (greeting, _) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Check - conditions were added
        CheckCondition(greeting.Topics[0].TopicInfos[3].ExtraConditions[^2], Skyrim.Race.NordRace.FormKey);
        CheckCondition(greeting.Topics[0].TopicInfos[3].ExtraConditions[^1], Skyrim.Race.NordRaceVampire.FormKey);

        // Check - note is gone
        greeting.Topics[0].TopicInfos[3].Responses[0]
            .StartNotes.Exists(x => x.Text == "if player is a Nord").Should().BeFalse();

        void CheckCondition(Condition condition, FormKey formKey) {
            var data = condition.Data.Should().BeOfType<GetIsRaceConditionData>();
            data.Subject.Race.Link.FormKey.Should().Be(formKey);
            condition.Flags.Should().HaveFlag(Condition.Flag.OR);
        }
    }
}
