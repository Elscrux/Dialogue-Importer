using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Factory;

public sealed class TestGreetingFactory {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestGreetingFactoryCreate() {
        // Import
        var (greeting, _) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Implement
        Conversation conversation = [greeting];
        conversation.Create();

        // Check
        _testConstants.Mod.DialogTopics.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.First().Responses.Should().HaveCount(7);
    }

    [Fact]
    public void TestGreeting() {
        // Import
        var (greetings, _, _) = TestSamples.GetBrinaCrossDialogue(_testConstants);

        greetings.Topics[0].TopicInfos.Should().HaveCount(2);
        foreach (var topicInfo in greetings.Topics[0].TopicInfos) {
            topicInfo.Responses.Should().ContainSingle();
        }

        // Process
        Conversation conversation = [greetings];
        _testConstants.DialogueProcessor.Process(conversation);

        var secondResponse = conversation[0].Topics[0].TopicInfos[1].Responses[0];
        secondResponse.Response.Should().NotContain("Always happy to see a customer.");
        secondResponse.ScriptNote.Should().Be(string.Empty);

        // Implement
        conversation.Create();

        // Check
        _testConstants.Mod.DialogTopics.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.First().Responses.Should().HaveCount(2);
    }
}
