using DialogueImplementationTool.Dialogue;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Factory;

public sealed class TestGreetingFactory {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestGreetingFactoryCreate() {
        var topic = TestDialogue.GetGreetingTopicCraneShore1();

        var greetingFactory = new Greeting(_testConstants.SkyrimDialogueContext);
        greetingFactory.GenerateDialogue(_testConstants.Quest, [topic]);

        topic.TopicInfos[0].Responses.Should().HaveCount(6);
    }
}
