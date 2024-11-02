using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestLockUnlockProcessor {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestDialogueTopicCraneShore1() {
        // Import
        var dialogue = TestSamples.GetLockTestDialogue(_testConstants);
        

        // Check
        foreach (var topic in dialogue.Topics) {
            topic.TopicInfos.Should().ContainSingle();
            var firstTopic = topic.TopicInfos[0];
            firstTopic.Prompt.StartNotes.Should().HaveCountGreaterThanOrEqualTo(1);
            firstTopic.Prompt.EndsNotes.Should().BeEmpty();
            firstTopic.ExtraConditions.Should().BeEmpty();
            firstTopic.Responses.Should().ContainSingle();
            firstTopic.Responses[0].StartNotes.Should().BeEmpty();
            firstTopic.Responses[0].EndsNotes.Should().ContainSingle();
        }

        // Process
        var lockUnlockProcessor = new DialogueQuestLockUnlockProcessor(_testConstants.SkyrimDialogueContext);
        lockUnlockProcessor.Process([dialogue]);

        // Check
        foreach (var topic in dialogue.Topics) {
            topic.TopicInfos.Should().ContainSingle();
            var firstTopic = topic.TopicInfos[0];
            firstTopic.Prompt.StartNotes.Should().BeEmpty();
            firstTopic.Prompt.EndsNotes.Should().BeEmpty();
            firstTopic.ExtraConditions.Should().ContainSingle();
            firstTopic.Responses.Should().ContainSingle();
            firstTopic.Responses[0].StartNotes.Should().BeEmpty();
            firstTopic.Responses[0].EndsNotes.Should().BeEmpty();
        }
    }
}
