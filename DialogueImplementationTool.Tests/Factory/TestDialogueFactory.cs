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
        failureTopicInfo.Links.Should().HaveElementAt(0, fishingGearTopic);

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

    [Fact]
    public void TestMultiLevelConditionDialogue() {
        // Import
        var dialogue = TestSamples.GetMultiLevelConditionDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogue];
        _testConstants.DialogueProcessor.Process(conversation);

        // Check
        conversation[0].Topics.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos.Should().HaveCount(3);

        var firstTopicInfo = conversation[0].Topics[0].TopicInfos[0];
        firstTopicInfo.Responses.Should().HaveCount(2);
        firstTopicInfo.Responses[0].HasNote("if SABOTAGED").Should().BeTrue();
        firstTopicInfo.Responses[0].Response.Should().Be("Well, just look at what happened in Hero's Rest recently. That should tell you all you need to know.");
        firstTopicInfo.Links.Should().ContainSingle();
        firstTopicInfo.Links[0].TopicInfos.Should().ContainSingle();
        firstTopicInfo.Links[0].TopicInfos[0].Responses.Should().HaveCount(2);
        firstTopicInfo.Links[0].GetPlayerText().Should().Be("Test");

        var secondTopicInfo = conversation[0].Topics[0].TopicInfos[1];
        secondTopicInfo.Responses.Should().ContainSingle();
        secondTopicInfo.Responses[0].HasNote("if HELPED").Should().BeTrue();
        secondTopicInfo.Responses[0].Response.Should().Be("Oh, I'd rather not go into that. It's not my place to discuss such things.");
        secondTopicInfo.Links.Should().BeEmpty();

        var thirdTopicInfo = conversation[0].Topics[0].TopicInfos[2];
        thirdTopicInfo.Responses.Should().HaveCount(5);
        thirdTopicInfo.Responses[0].HasNote("else").Should().BeTrue();
        thirdTopicInfo.Responses[0].Response.Should().Be("Well, I'm... glad you asked.");
        thirdTopicInfo.Links.Should().HaveCount(3);

        var firstLink = thirdTopicInfo.Links[0];
        firstLink.TopicInfos.Should().ContainSingle();
        firstLink.TopicInfos[0].Responses.Should().HaveCount(2);
        firstLink.TopicInfos[0].Links.Should().HaveCount(2);

        var secondLink = thirdTopicInfo.Links[1];
        secondLink.TopicInfos.Should().ContainSingle();
        secondLink.TopicInfos[0].Responses.Should().HaveCount(2);
        secondLink.TopicInfos[0].Links.Should().BeEmpty();


        var thirdLink = thirdTopicInfo.Links[2];
        thirdLink.TopicInfos.Should().ContainSingle();
        thirdLink.TopicInfos[0].Responses.Should().ContainSingle();
        thirdLink.TopicInfos[0].Links.Should().BeEmpty();
    }
}
