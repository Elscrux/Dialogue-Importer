using DialogueImplementationTool.Dialogue;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Factory;

public sealed class TestDialogueFactory {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestGreetingFactoryCreate() {
        List<DialogueTopic> topics = [TestDialogue.GetGreetingTopicCraneShore1()];

        var greetingFactory = new Greeting(_testConstants.SkyrimDialogueContext);
        greetingFactory.PreProcess(topics);
        greetingFactory.GenerateDialogue(_testConstants.Quest, topics);
        greetingFactory.PostProcess();

        _testConstants.Mod.DialogTopics.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.First().Responses.Should().HaveCount(6);
    }
}
