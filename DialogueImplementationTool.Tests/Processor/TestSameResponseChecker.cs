using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSameResponseChecker {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestDialogueTopicCraneShore1() {
        // Import
        var (_, dialogue) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Check
        dialogue.Topics[0].TopicInfos[0].Links[0].TopicInfos[0].Responses.Should().HaveCount(0);

        // Process
        Conversation conversation = [dialogue];
        var sameResponseChecker = new SameResponseChecker();
        sameResponseChecker.Process(conversation);

        // Check
        conversation[0].Topics[0].TopicInfos[0].Links[0].TopicInfos[0].Responses.Should().ContainSingle();
    }
}
