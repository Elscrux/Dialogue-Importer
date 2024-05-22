using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Factory;

public sealed class TestDialogueFactory {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestBrinaCrossDialogue() {
        // Import
        var (_, dialogueTopics, _) = TestSamples.GetBrinaCrossDialogue(_testConstants);

        // Check
        dialogueTopics.Topics.Should().HaveCount(2);
        dialogueTopics.Topics[0].TopicInfos[0].Responses.Should().HaveCount(3);
        dialogueTopics.Topics[1].TopicInfos[0].Links.Should().HaveCount(2);

        // Process
        Conversation conversation = [dialogueTopics];
        _testConstants.DialogueProcessor.Process(conversation);

        // Check
        conversation[0].Topics[0].TopicInfos[0].Responses[1].ScriptNote.Should().Be("emphasis: cosmopolitan");
        conversation[0].Topics[0].TopicInfos[0].Responses[2].HasNote("back to options").Should().BeFalse();
        conversation[0].Topics[0].TopicInfos[0].Links.Should().BeEmpty();
        conversation[0].Topics[0].TopicInfos[0].Random.Should().BeFalse();

        var links = conversation[0].Topics[1].TopicInfos[0].Links;
        links[0].TopicInfos[0].Links.Should().HaveCount(2);
        links[1].TopicInfos[0].Links.Should().BeEmpty();

        // Implement
        conversation.Create();

        // Check
        _testConstants.Mod.DialogTopics.Should().HaveCount(4);
        _testConstants.Mod.DialogTopics.First().EditorID.Should().EndWith("1Topic");
        _testConstants.Mod.DialogTopics.Skip(1).First().EditorID.Should().EndWith("2Topic");
        _testConstants.Mod.DialogTopics.Skip(2).First().EditorID.Should().EndWith("2TopicA");
        _testConstants.Mod.DialogTopics.Skip(3).First().EditorID.Should().EndWith("2TopicB");
    }

    [Fact]
    public void TestCraneShoreDialogue() {
        // Import
        var (_, dialogue) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogue];
        _testConstants.DialogueProcessor.Process(conversation);

        // Implement
        conversation.Create();

        // Check
        _testConstants.Mod.DialogBranches.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.Should().HaveCount(7);

        var firstTopic = _testConstants.Mod.DialogTopics.First();
        firstTopic.Responses.Should().ContainSingle();
        firstTopic.Name!.String.Should().Be("What can you tell me about Crane Shore?");
        firstTopic.Responses[0].Responses.Should().ContainSingle();
        firstTopic.Responses[0].LinkTo.Should().HaveCount(4);

        var illusionTopic = _testConstants.Mod.DialogTopics.Skip(4).First();
        illusionTopic.Responses.Should().HaveCount(2);
        illusionTopic.Name!.String.Should()
            .Be("There's no need to be like that. We can be friends. (Illusion) [Illusion 40]");
        illusionTopic.Responses[0].Responses.Should().ContainSingle();
        illusionTopic.Responses[1].Responses.Should().ContainSingle();

        var persuadeTopic = _testConstants.Mod.DialogTopics.Skip(5).First();
        persuadeTopic.Responses.Should().HaveCount(2);
        persuadeTopic.Name!.String.Should().Be("You think Crane Shore could be more than it is? (Persuade) [average]");
        persuadeTopic.Responses[0].Responses.Should().HaveCount(3);
        persuadeTopic.Responses[1].Responses.Should().HaveCount(2);
        persuadeTopic.Responses[0].LinkTo.Should().ContainSingle();
        persuadeTopic.Responses[1].LinkTo.Should().ContainSingle();
    }

    [Fact]
    public void TestMalwonDialogue() {
        // Import
        var (_, _, dialogue) = TestSamples.GetMalwonDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogue];
        _testConstants.DialogueProcessor.Process(conversation);

        // Check structure
        conversation[0].Topics.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos.Should().ContainSingle();

        var rootTopicInfo = conversation[0].Topics[0].TopicInfos[0];
        rootTopicInfo.Links.Should().HaveCount(3);
        var fishingGearTopic = rootTopicInfo.Links[0];
        fishingGearTopic.TopicInfos.Should().ContainSingle();
        fishingGearTopic.TopicInfos[0].Links.Should().HaveCount(3);
        var archeryTopic = fishingGearTopic.TopicInfos[0].Links[0];
        archeryTopic.TopicInfos.Should().HaveCount(2);
        var successTopicInfo = archeryTopic.TopicInfos[0];
        successTopicInfo.Links.Should().BeEmpty();
        var failureTopicInfo = archeryTopic.TopicInfos[1];
        failureTopicInfo.Links.Should().HaveCount(3);

        var nestedIntimidateTopic = fishingGearTopic.TopicInfos[0].Links[1];
        nestedIntimidateTopic.TopicInfos.Should().HaveCount(2);

        var nestedPersuadeTopic = fishingGearTopic.TopicInfos[0].Links[2];
        nestedPersuadeTopic.TopicInfos.Should().HaveCount(2);

        var intimidateTopic = rootTopicInfo.Links[1];
        intimidateTopic.TopicInfos.Should().HaveCount(2);

        var persuadeTopic = rootTopicInfo.Links[2];
        persuadeTopic.TopicInfos.Should().HaveCount(2);

        // Implement
        conversation.Create();

        // Check shared conditions
        var sharedInfo = _testConstants.Mod.DialogTopics
            .First(x => x.EditorID is not null && x.EditorID.EndsWith("Shared"));

        foreach (var response in sharedInfo.Responses) {
            response.Conditions.Should().ContainSingle();
        }
    }
}
