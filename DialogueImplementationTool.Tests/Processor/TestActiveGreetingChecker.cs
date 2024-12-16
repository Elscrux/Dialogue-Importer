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
        if (greeting.Topics[0].TopicInfos[2].ExtraConditions.Count >= 1)
            CheckCondition(greeting.Topics[0].TopicInfos[2].ExtraConditions[^1]);

        // Check - note is gone
        greeting.Topics[0].TopicInfos[2].Responses[0]
            .StartNotes.Exists(x => x.Text == "active").Should().BeFalse();

        void CheckCondition(Condition condition) {
            condition.Data.Should().BeOfType<IsInDialogueWithPlayerConditionData>();
        
        }
    }
}