using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Factory;

public sealed class TestGenericSceneFactory {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestScene() {
        // Import
        var scene1 = TestSamples.GetBrinaCrossScenes(_testConstants, 0);

        // Check
        scene1.Topics.Should().HaveCount(6);

        // Process
        Conversation conversation = [scene1];
        _testConstants.DialogueProcessor.Process(conversation);

        // Check
        var secondResponse = conversation[0].Topics[1].TopicInfos[0].Responses[0];
        secondResponse.FullResponse.Should().NotContain("morose");
        secondResponse.ScriptNote.Should().Be("morose");

        // Implement
        scene1.Create();

        // Check
        _testConstants.Mod.Scenes.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.Should().HaveCount(6);
        foreach (var dialogTopic in _testConstants.Mod.DialogTopics) {
            dialogTopic.Responses.Should().ContainSingle();
        }
    }

    [Fact]
    public void TestSceneWithBranches() {
        // Import
        var (scene1, scene2) = TestSamples.GetOldwallScenes(_testConstants);

        // Check
        scene1.Topics.Should().HaveCount(5);
        scene2.Topics.Should().HaveCount(5);

        // Process
        Conversation conversation = [scene1, scene2];
        _testConstants.DialogueProcessor.Process(conversation);

        // Check
        conversation[0].Topics.Should().HaveCount(5);
        conversation[1].Topics.Should().HaveCount(5);

        // Implement
        conversation.Create();

        // Check
        _testConstants.Mod.Scenes.Should().HaveCount(2);
        _testConstants.Mod.DialogTopics.Should().HaveCount(13); // 6 + 6 + 1 (shared dialogue)
    }

    [Fact]
    public void TestPreprocessing() {
        // Import
        var (scene1, scene2) = TestSamples.GetOldwallScenes(_testConstants);

        // Check - 8 individual lines each with 3 responses merged each
        scene1.Topics.Should().HaveCount(5);
        scene2.Topics.Should().HaveCount(5);
        scene1.Topics[1].TopicInfos[0].Responses.Should().HaveCount(2);
        scene1.Topics[1].TopicInfos[0].Links.Should().BeEmpty();

        // Process conversation
        Conversation conversation = [scene1, scene2];
        _testConstants.DialogueProcessor.Process(conversation);

        // Check - second topic should be split in shared info processing
        scene1.Topics.Should().HaveCount(5);
        scene2.Topics.Should().HaveCount(5);
        scene1.Topics[1].TopicInfos[0].SharedInfo.Should().NotBeNull();
        scene1.Topics[1].TopicInfos[0].SharedInfo!.ResponseDataTopicInfo.Responses.Should().ContainSingle();
        scene1.Topics[1].TopicInfos[0].Links.Should().ContainSingle();

        // Process
        var genericSceneFactory = new GenericSceneFactory(_testConstants.SkyrimDialogueContext);
        genericSceneFactory.PreProcess(scene1.Topics);
        genericSceneFactory.PreProcess(scene2.Topics);

        // Check - shared line link should be flattened, + 1 topic
        scene1.Topics.Should().HaveCount(6);
        scene2.Topics.Should().HaveCount(6);
    }
}
