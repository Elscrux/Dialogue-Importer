using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Processor;

public class TestActiveGreetingChecker {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestGreetingCreate() {
        // Import
        var (greeting, _) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Check - condition was added
        var topicInfo = greeting.Topics[0].TopicInfos[2];
        if (topicInfo.ExtraConditions.Count >= 1) {
            topicInfo.ExtraConditions[^1].Data.Should().BeOfType<IsInDialogueWithPlayerConditionData>();
        }

        // Check - note is gone
        topicInfo.Responses[0].StartNotes.Exists(x => x.Text == "active").Should().BeFalse();
    }
}