using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSayOnceChecker {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestGreetingFactoryCreate() {
        // Import
        var (greeting, _) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Check
        greeting.Topics[0].TopicInfos.Should().HaveCount(7);

        // Process
        var sameResponseChecker = new SayOnceChecker();
        foreach (var topicInfo in greeting.Topics[0].TopicInfos) {
            sameResponseChecker.Process(topicInfo);
        }

        // Check
        greeting.Topics[0].TopicInfos.Should().HaveCount(7);

        // First two topics should be say once
        foreach (var topicInfo in greeting.Topics[0].TopicInfos.Take(2)) {
            topicInfo.SayOnce.Should().BeTrue();
            topicInfo.Responses.Should().ContainSingle();
        }

        // The rest should not be say once
        foreach (var topicInfo in greeting.Topics[0].TopicInfos.Skip(2)) {
            topicInfo.SayOnce.Should().BeFalse();
            topicInfo.Responses.Should().ContainSingle();
        }
    }
}
