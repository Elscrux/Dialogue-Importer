using DialogueImplementationTool.Dialogue.Processor;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSayOnceChecker {
    [Fact]
    public void TestGreetingFactoryCreate() {
        var greeting = TestDialogue.GetGreetingTopicCraneShore1();

        greeting.TopicInfos.Should().HaveCount(7);

        var sameResponseChecker = new SayOnceChecker();
        foreach (var topicInfo in greeting.TopicInfos) {
            sameResponseChecker.Process(topicInfo);
        }

        greeting.TopicInfos.Should().HaveCount(7);

        // First two topics should be say once
        foreach (var topicInfo in greeting.TopicInfos.Take(2)) {
            topicInfo.SayOnce.Should().BeTrue();
            topicInfo.Responses.Should().ContainSingle();
            topicInfo.Responses[0]
                .Response.Should()
                .Be("Come on in and look around. We stock everything the Company tells us to stock, and then some.");
        }

        // The rest should not be say once
        foreach (var topicInfo in greeting.TopicInfos.Skip(2)) {
            topicInfo.SayOnce.Should().BeFalse();
        }
    }
}
