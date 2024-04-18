using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
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

    [Fact]
    public void TestDialogueFactoryCreate() {
        var topic = TestDialogue.GetDialogueTopicCraneShore1();
        var generatedDialogue = TestDialogue.TopicAsGeneratedDialogue(topic);
        _testConstants.ProcessEverything(generatedDialogue);

        var greetingFactory = new Dialogue.Dialogue(_testConstants.SkyrimDialogueContext);
        greetingFactory.PreProcess(generatedDialogue[0].Topics);
        greetingFactory.GenerateDialogue(_testConstants.Quest, generatedDialogue[0].Topics);
        greetingFactory.PostProcess();

        _testConstants.Mod.DialogBranches.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.Should().HaveCount(8);

        var firstTopic = _testConstants.Mod.DialogTopics.First();
        firstTopic.Responses.Should().ContainSingle();
        firstTopic.Name!.String.Should().Be("What can you tell me about Crane Shore?");
        firstTopic.Responses[0].Responses.Should().ContainSingle();
        firstTopic.Responses[0].LinkTo.Should().HaveCount(4);

        var illusionTopic = _testConstants.Mod.DialogTopics.Skip(4).First();
        illusionTopic.Responses.Should().HaveCount(2);
        illusionTopic.Name!.String.Should().Be("There's no need to be like that. We can be friends. (Illusion) [Hard]?");
        illusionTopic.Responses[0].Responses.Should().ContainSingle();
        illusionTopic.Responses[1].Responses.Should().ContainSingle();
    }
}
