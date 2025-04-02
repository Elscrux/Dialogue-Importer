using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Factory;

public sealed class TestGenericScene3x3Factory {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestScene3x3() {
        // Import
        var scene3 = TestSamples.GetBrinaCrossScenes(_testConstants, 2);

        // Check
        scene3.Topics.Should().HaveCount(3);
        scene3.Topics[0].TopicInfos[0].Responses.Should().HaveCount(3);
        scene3.Topics[1].TopicInfos[0].Responses.Should().HaveCount(2);
        scene3.Topics[2].TopicInfos[0].Responses.Should().HaveCount(3);

        // Process
        Conversation conversation = [scene3];
        _testConstants.DialogueProcessor.Process(conversation);

        // Check
        var firstResponse = conversation[0].Topics[0].TopicInfos[0].Responses[0];
        firstResponse.FullResponse.Should().NotContain("tone: happy");
        firstResponse.ScriptNote.Should().Be("tone: happy");

        var secondResponse = conversation[0].Topics[0].TopicInfos[0].Responses[1];
        secondResponse.FullResponse.Should().Be("[belongs to previous line] Anything special?");

        var secondTopic = conversation[0].Topics[1].TopicInfos[0].Responses[0];
        secondTopic.Response.Should().NotContain("nervous");
        secondTopic.ScriptNote.Should().Be("nervous");

        // Implement
        conversation.Create();

        // Check
        _testConstants.Mod.Scenes.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.Should().HaveCount(3);
        _testConstants.Mod.DialogTopics.First().Responses.Should().HaveCount(2);
        _testConstants.Mod.DialogTopics.First().Responses.First().Responses.Should().HaveCount(2);
        _testConstants.Mod.DialogTopics.First().Responses.Skip(1).First().Responses.Should().HaveCount(1);
        _testConstants.Mod.DialogTopics.Skip(1).First().Responses.Should().HaveCount(2);
        _testConstants.Mod.DialogTopics.Skip(2).First().Responses.Should().HaveCount(3);
        foreach (var dialogResponses in _testConstants.Mod.DialogTopics.SelectMany(topic => topic.Responses)) {
            dialogResponses.Flags.Should().NotBeNull();
            dialogResponses.Flags!.Flags.Should().HaveFlag(DialogResponses.Flag.Random);
        }
    }
}
