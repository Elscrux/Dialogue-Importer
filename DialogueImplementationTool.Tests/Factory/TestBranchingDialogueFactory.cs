using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Factory;

public sealed class TestBranchingDialogueFactory {
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
        _testConstants.Quest.EditorID = "DialogueQuest";
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

        var hostile1Topic = _testConstants.Mod.DialogTopics.Skip(2).First();
        hostile1Topic.Name!.String.Should().Be("Are you always so hostile?");
        hostile1Topic.Responses.Should().ContainSingle();
        hostile1Topic.Responses[0].ResponseData.IsNull.Should().BeFalse();

        var hostile2Topic = _testConstants.Mod.DialogTopics.Skip(3).First();
        hostile2Topic.Name!.String.Should().Be("Why so hostile?");
        hostile2Topic.Responses.Should().ContainSingle();
        hostile2Topic.Responses[0].ResponseData.IsNull.Should().BeFalse();

        var illusionTopic = _testConstants.Mod.DialogTopics.Skip(4).First();
        illusionTopic.Responses.Should().HaveCount(2);
        illusionTopic.Name!.String.Should()
            .Be("There's no need to be like that. We can be friends. (Illusion) [Illusion 40]");
        illusionTopic.Responses[0].Responses.Should().ContainSingle();
        illusionTopic.Responses[1].Responses.Should().ContainSingle();

        var persuadeTopic = _testConstants.Mod.DialogTopics.Skip(5).First();
        persuadeTopic.Responses.Should().HaveCount(2);
        persuadeTopic.Name!.String.Should().Be("You think Crane Shore could be more than it is? (Persuade)");
        persuadeTopic.Responses[0].Responses.Should().HaveCount(3);
        persuadeTopic.Responses[1].Responses.Should().HaveCount(2);
        persuadeTopic.Responses[0].LinkTo.Should().ContainSingle();
        persuadeTopic.Responses[1].LinkTo.Should().ContainSingle();
        persuadeTopic.Responses[0].Conditions.Should().HaveCount(3);
        persuadeTopic.Responses[1].Conditions.Should().ContainSingle();

        // Check both persuade options link to the same final line
        persuadeTopic.Responses[1].LinkTo[0].FormKey.Should().Be(persuadeTopic.Responses[0].LinkTo[0].FormKey);

        var persuadeFinalTopic = _testConstants.Mod.DialogTopics.Skip(6).First();
        persuadeFinalTopic.Responses.Should().ContainSingle();
        persuadeFinalTopic.Responses[0].Responses[0].Text.String.Should().Be("Anyway, why are we talking again?");
    }

    [Fact]
    public void TestMalwonDialogue() {
        // Import
        var (_, _, dialogue, dialogue2) = TestSamples.GetMalwonDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogue, dialogue2];
        dialogue.Factory
            .ConfigureProcessor(_testConstants.DialogueProcessor)
            .Process(conversation);

        // Check structure
        conversation[0].Topics.Should().ContainSingle();
        conversation[0].Topics[0].Blocking.Should().BeFalse();
        conversation[0].Topics[0].TopicInfos.Should().ContainSingle();

        var rootTopicInfo = conversation[0].Topics[0].TopicInfos[0];
        rootTopicInfo.SharedInfo.Should().BeNull();
        rootTopicInfo.Links.Should().HaveCount(3);
        var fishingGearTopic = rootTopicInfo.Links[0];
        fishingGearTopic.TopicInfos.Should().ContainSingle();
        fishingGearTopic.TopicInfos[0].Links.Should().HaveCount(3);
        fishingGearTopic.TopicInfos[0].SharedInfo.Should().BeNull();

        var archeryTopic = fishingGearTopic.TopicInfos[0].Links[0];
        archeryTopic.TopicInfos.Should().HaveCount(2);
        archeryTopic.TopicInfos[0].SharedInfo.Should().NotBeNull();
        archeryTopic.TopicInfos[0].Prompt.FullText.Should().Be("I don't see any hunting gear either. (Archery) [easy]");
        archeryTopic.TopicInfos[0].ExtraConditions.Should().HaveCount(4);
        foreach (var extraCondition in archeryTopic.TopicInfos[0].ExtraConditions) {
            extraCondition.Data.Should().BeOfType<GetPCIsRaceConditionData>();
            extraCondition.Flags.HasFlag(Condition.Flag.OR).Should().BeFalse();
        }
        archeryTopic.TopicInfos[1].SharedInfo.Should().BeNull();

        var successTopicInfo = archeryTopic.TopicInfos[0];
        successTopicInfo.Links.Should().HaveCount(3);
        successTopicInfo.SharedInfo.Should().NotBeNull();

        var failureTopicInfo = archeryTopic.TopicInfos[1];
        failureTopicInfo.Links.Should().HaveCount(3);
        failureTopicInfo.Links.Should().HaveElementAt(0, fishingGearTopic);
        failureTopicInfo.SharedInfo.Should().BeNull();
        failureTopicInfo.Script.StartScriptLines.Should().BeEmpty();

        var nestedIntimidateTopic = fishingGearTopic.TopicInfos[0].Links[1];
        nestedIntimidateTopic.TopicInfos.Should().HaveCount(2);
        nestedIntimidateTopic.TopicInfos[0].SharedInfo.Should().NotBeNull();
        nestedIntimidateTopic.TopicInfos[1].SharedInfo.Should().NotBeNull();

        var nestedPersuadeTopic = fishingGearTopic.TopicInfos[0].Links[2];
        nestedPersuadeTopic.TopicInfos.Should().HaveCount(2);
        nestedPersuadeTopic.TopicInfos[0].SharedInfo.Should().NotBeNull();
        nestedPersuadeTopic.TopicInfos[1].SharedInfo.Should().NotBeNull();

        var intimidateTopic = rootTopicInfo.Links[1];
        intimidateTopic.TopicInfos.Should().HaveCount(2);

        intimidateTopic.TopicInfos[0].Prompt.FullText.Should()
            .Be("Tell me what's going on and I won't hurt anyone. (Intimidate)");
        intimidateTopic.TopicInfos[0].SharedInfo.Should().NotBeNull();
        intimidateTopic.TopicInfos[0].ExtraConditions.Should().ContainSingle();
        intimidateTopic.TopicInfos[0].ExtraConditions[0].Data.Should().BeOfType<GetIntimidateSuccessConditionData>();

        intimidateTopic.TopicInfos[1].Prompt.FullText.Should()
            .Be("Tell me what's going on and I won't hurt anyone. (Intimidate)");
        intimidateTopic.TopicInfos[1].ExtraConditions.Should().BeEmpty();
        intimidateTopic.TopicInfos[1].SharedInfo.Should().NotBeNull();

        var persuadeTopic = rootTopicInfo.Links[2];
        persuadeTopic.TopicInfos.Should().HaveCount(2);
        persuadeTopic.TopicInfos[0].SharedInfo.Should().NotBeNull();
        persuadeTopic.TopicInfos[0].ExtraConditions.Should().HaveCount(2);
        persuadeTopic.TopicInfos[0].ExtraConditions[0].Data.Should().BeOfType<GetActorValueConditionData>();
        persuadeTopic.TopicInfos[1].SharedInfo.Should().NotBeNull();
        persuadeTopic.TopicInfos[1].ExtraConditions.Should().BeEmpty();


        conversation[1].Topics.Should().ContainSingle();
        conversation[1].Topics[0].Blocking.Should().BeTrue();

        var continueTopicInfo = conversation[1].Topics[0].TopicInfos[0];
        continueTopicInfo.Links.Should().HaveCount(3);
        continueTopicInfo.SharedInfo.Should().NotBeNull();
        continueTopicInfo.SharedInfo!.ResponseDataTopicInfo.Responses.Should().HaveCount(2);

        var notHereForYouTopic = continueTopicInfo.Links[0];
        notHereForYouTopic.TopicInfos.Should().ContainSingle();
        notHereForYouTopic.TopicInfos[0].Links.Should().HaveCount(3);
        notHereForYouTopic.TopicInfos[0].SharedInfo.Should().BeNull();

        var notForYouToKnowTopic = continueTopicInfo.Links[1];
        notForYouToKnowTopic.TopicInfos.Should().ContainSingle();
        notForYouToKnowTopic.TopicInfos[0].Links.Should().BeEmpty();
        notForYouToKnowTopic.TopicInfos[0].SharedInfo.Should().NotBeNull();

        var askingQuestionsTopics = continueTopicInfo.Links[2];
        askingQuestionsTopics.TopicInfos.Should().ContainSingle();
        askingQuestionsTopics.TopicInfos[0].Links.Should().BeEmpty();
        askingQuestionsTopics.TopicInfos[0].SharedInfo.Should().NotBeNull();

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
    public void TestIdonaVerusDialogue() {
        // Import as dialogue quest
        var (_, dialogue, _) = TestSamples.GetIdonaVerusDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogue];
        _testConstants.Quest.EditorID = "DialogueQuest";
        _testConstants.DialogueProcessor.Process(conversation);

        // Check structure
        conversation[0].Topics.Should().HaveCount(4);

        conversation[0].Topics[0].TopicInfos[0].ExtraConditions.Should().ContainSingle();
        var x = conversation[0].Topics[0].TopicInfos[0].ExtraConditions[0].Data.Should().BeOfType<GetStageDoneConditionData>();
        x.Subject.Stage.Should().Be(12);

        conversation[0].Topics[1].TopicInfos[0].ExtraConditions.Should().BeEmpty();
        conversation[0].Topics[1].TopicInfos[0].Script.StartScriptLines.Should().HaveCount(4);

        conversation[0].Topics[2].TopicInfos[0].ExtraConditions.Should().ContainSingle();
        x = conversation[0].Topics[2].TopicInfos[0].ExtraConditions[0].Data.Should().BeOfType<GetStageDoneConditionData>();
        x.Subject.Stage.Should().Be(10);
        conversation[0].Topics[2].TopicInfos[0].ExtraConditions.Should().ContainSingle();

        conversation[0].Topics[3].TopicInfos[0].ExtraConditions.Should().ContainSingle();
        x = conversation[0].Topics[3].TopicInfos[0].ExtraConditions[0].Data.Should().BeOfType<GetStageDoneConditionData>();
        x.Subject.Stage.Should().Be(11);
    }

    [Fact]
    public void TestAdilaNadeDialogue() {
        // Import as dialogue quest
        _testConstants.FormKeySelection = new InjectedFormKeySelection(new Dictionary<string, FormKey> {
            { "Select: Marille Nade", _testConstants.Speaker1.FormLink.FormKey },
            { "Select: Ezzib Nade", _testConstants.Speaker2.FormLink.FormKey },
        });
        var (greeting, dialogue1, dialogue2, farewell) = TestSamples.GetAdilaNadeDialogue(_testConstants);

        // Process
        Conversation conversation = [greeting, dialogue1, dialogue2, farewell];
        _testConstants.Quest.EditorID = "DialogueQuest";
        _testConstants.DialogueProcessor.Process(conversation);

        // Check structure
        conversation[0].Topics[0].TopicInfos.Should().HaveCount(7);
        conversation[0].Topics[0].TopicInfos[0].ExtraConditions.Should().BeEmpty();
        conversation[0].Topics[0].TopicInfos[1].ExtraConditions.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos[2].ExtraConditions.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos[3].ExtraConditions.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos[4].ExtraConditions.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos[5].ExtraConditions.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos[6].ExtraConditions.Should().ContainSingle();

        conversation[1].Topics.Should().HaveCount(2);

        // [FEAR]
        conversation[1].Topics[0].TopicInfos[0].ExtraConditions.Should().ContainSingle();
        var stageCondition = conversation[1].Topics[0].TopicInfos[0].ExtraConditions[0].Data.Should()
            .BeOfType<GetStageDoneConditionData>();
        stageCondition.Subject.Stage.Should().Be(10);

        // [unlock KID]
        var topic = conversation[1].Topics[0].TopicInfos[0].Links[1];
        topic.TopicInfos[0].Script.StartScriptLines.Should().BeEmpty();

        topic.TopicInfos.Should().HaveCount(2);
        topic.TopicInfos[0].Links.Should().ContainSingle();
        topic.TopicInfos[0].ExtraConditions.Should().HaveCount(2);
        topic.TopicInfos[1].Responses.Should().ContainSingle();
        topic.TopicInfos[1].Responses[0].Response.Should().BeEmpty();

        topic.TopicInfos[0].Links[0].TopicInfos.Should().ContainSingle();
        topic.TopicInfos[0].Links[0].TopicInfos[0].Links.Should().ContainSingle();
        topic.TopicInfos[0].Links[0].TopicInfos[0].ExtraConditions.Should().BeEmpty();

        topic.TopicInfos[0].Links[0].TopicInfos[0].Links[0].TopicInfos.Should().ContainSingle();
        topic.TopicInfos[0].Links[0].TopicInfos[0].Links[0].TopicInfos[0].Links.Should().BeEmpty();
        topic.TopicInfos[0].Links[0].TopicInfos[0].Links[0].TopicInfos[0].ExtraConditions.Should().ContainSingle();

        // [REASON]
        conversation[1].Topics[1].TopicInfos[0].ExtraConditions.Should().ContainSingle();

        // Persuade
        conversation[1].Topics[1].TopicInfos[0].Links[1].TopicInfos.Should().HaveCount(2);
        var successInfo = conversation[1].Topics[1].TopicInfos[0].Links[1].TopicInfos[0];
        successInfo.Script.StartScriptLines.Should().ContainSingle();
        successInfo.Script.StartScriptLines[0].Should().Be("pFDS.Persuade(akSpeaker)");
        successInfo.ExtraConditions.Should().HaveCount(2);
        var actorValueCondition = successInfo.ExtraConditions[0].Data.Should().BeOfType<GetActorValueConditionData>();
        actorValueCondition.Subject.ActorValue.Should().Be(ActorValue.Speech);
        successInfo.Links.Should().ContainSingle();

        // [LEARN] <- this is a keyword link, not unlock
        conversation[2].Topics[0].TopicInfos[0].ExtraConditions.Should().BeEmpty();

        // [unlock all BRAVE] [lock all FEAR, REASON]
        var links = conversation[2].Topics[0].TopicInfos[0].Links[0].TopicInfos[0].Links;
        links.Should().HaveCount(2);
        links[0].TopicInfos[0].Links[0].TopicInfos[0].Links[0].TopicInfos[0].Script.StartScriptLines.Should().HaveCount(6);

        // [unlock all BRAVE in Adila and Marille's dialogue] [lock all FEAR, REASON]
        links[1].TopicInfos[0].Links[0].TopicInfos[0].Links[0].TopicInfos[0].Script.StartScriptLines.Should().HaveCount(6);
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
        firstTopicInfo.Responses[0].Response.Should()
            .Be("Well, just look at what happened in Hero's Rest recently. That should tell you all you need to know.");
        firstTopicInfo.Links.Should().ContainSingle();
        firstTopicInfo.Links[0].TopicInfos.Should().ContainSingle();
        firstTopicInfo.Links[0].TopicInfos[0].Responses.Should().HaveCount(2);
        firstTopicInfo.Links[0].GetPlayerFullText().Should().Be("Test");

        var secondTopicInfo = conversation[0].Topics[0].TopicInfos[1];
        secondTopicInfo.Responses.Should().ContainSingle();
        secondTopicInfo.Responses[0].HasNote("if HELPED").Should().BeTrue();
        secondTopicInfo.Responses[0].Response.Should()
            .Be("Oh, I'd rather not go into that. It's not my place to discuss such things.");
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
        thirdLink.TopicInfos[0].SharedInfo.Should().NotBeNull();
        thirdLink.TopicInfos[0].Links.Should().BeEmpty();
    }

    [Fact]
    public void TestMediAtMuhayDialogue() {
        // Import
        var dialogue = TestSamples.GetMediAtMuhayDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogue];
        _testConstants.Quest.EditorID = "DialogueQuest";
        _testConstants.DialogueProcessor.Process(conversation);

        // Check
        conversation[0].Topics.Should().HaveCount(3);
        conversation[0].Topics[0].TopicInfos.Should().ContainSingle();
        var firstTopicInfo = conversation[0].Topics[0].TopicInfos[0];
        firstTopicInfo.Responses.Should().HaveCount(5);
        firstTopicInfo.Responses[1].HasNote(x => x == "unlock BUREAU").Should().BeFalse();

        var secondTopicInfo = conversation[0].Topics[1];
        secondTopicInfo.TopicInfos.Should().ContainSingle();
        secondTopicInfo.TopicInfos[0].Prompt.Notes().Should().BeEmpty();
        secondTopicInfo.TopicInfos[0].Responses.Should().HaveCount(4);
        secondTopicInfo.TopicInfos[0].Responses[^1].HasNote("back to root").Should().BeFalse();

        var thirdTopicInfo = conversation[0].Topics[2];
        thirdTopicInfo.TopicInfos.Should().ContainSingle();
        thirdTopicInfo.TopicInfos[0].Prompt.Notes().Should().BeEmpty();
        thirdTopicInfo.TopicInfos[0].Responses.Should().HaveCount(6);
        thirdTopicInfo.TopicInfos[0].Responses[^1].HasNote("back to root").Should().BeFalse();
    }

    [Fact]
    public void TestMaenlornDialogue() {
        // Import
        var dialogue = TestSamples.GetMaenlornDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogue];
        _testConstants.Quest.EditorID = "DialogueQuest";
        _testConstants.DialogueProcessor.Process(conversation);

        // Check
        conversation[0].Topics.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos.Should().ContainSingle();
        conversation[0].Topics[0].TopicInfos[0].Links.Should().HaveCount(6);
        conversation[0].Topics[0].TopicInfos[0].InvisibleContinue.Should().BeFalse();

        var firstLink = conversation[0].Topics[0].TopicInfos[0].Links[0];
        firstLink.TopicInfos.Should().ContainSingle();
        firstLink.TopicInfos[0].Responses.Should().ContainSingle();
        firstLink.TopicInfos[0].Responses[0].FullResponse.Should().Be(
            "Ahaha! I do love the realist answer. Though, riddles often have a way of bending the reality they present, don't they?");
        firstLink.TopicInfos[0].InvisibleContinue.Should().BeTrue();
        firstLink.TopicInfos[0].Links.Should().ContainSingle();

        var secondLink = firstLink.TopicInfos[0].Links[0];
        secondLink.TopicInfos.Should().ContainSingle();
        secondLink.TopicInfos[0].Responses.Should().ContainSingle();
        secondLink.TopicInfos[0].Responses[0].FullResponse.Should()
            .Be("The beggar, the thief, the warrior, and the king are all one in the same! Silly, isn't it?");
        secondLink.TopicInfos[0].InvisibleContinue.Should().BeTrue();
        secondLink.TopicInfos[0].Links.Should().ContainSingle();

        var thirdLink = secondLink.TopicInfos[0].Links[0];
        thirdLink.TopicInfos.Should().ContainSingle();
        thirdLink.TopicInfos[0].Responses.Should().HaveCount(2);
        thirdLink.TopicInfos[0].Responses[0].FullResponse.Should()
            .Be("The Adventures of Eslaf Erol, a classic tale of rags to riches.");
        thirdLink.TopicInfos[0].Responses[1].FullResponse.Should().Be("It reminds us of the importance of family.");
        thirdLink.TopicInfos[0].InvisibleContinue.Should().BeFalse();
        thirdLink.TopicInfos[0].Links.Should().HaveCount(2);

        var fourthLink = thirdLink.TopicInfos[0].Links[0];
        fourthLink.TopicInfos.Should().ContainSingle();
        fourthLink.TopicInfos[0].Responses.Should().ContainSingle();
        fourthLink.TopicInfos[0].Responses[0].FullResponse.Should().Be(
            "Many go through life without mother or father, brother or sister... All that matters is the family you have chosen.");
        fourthLink.TopicInfos[0].InvisibleContinue.Should().BeTrue();
        fourthLink.TopicInfos[0].Links.Should().ContainSingle();

        var fifthLink = fourthLink.TopicInfos[0].Links[0];
        fifthLink.TopicInfos.Should().ContainSingle();
        fifthLink.TopicInfos[0].Responses.Should().ContainSingle();
        fifthLink.TopicInfos[0].Responses[0].FullResponse.Should().Be("No one is ever truly alone. Remember that.");
        fifthLink.TopicInfos[0].InvisibleContinue.Should().BeFalse();
        fifthLink.TopicInfos[0].Links.Should().HaveCount(2);

        var sixthLink = fifthLink.TopicInfos[0].Links[0];
        sixthLink.TopicInfos.Should().ContainSingle();
        sixthLink.TopicInfos[0].Responses.Should().ContainSingle();
        sixthLink.TopicInfos[0].Responses[0].FullResponse.Should().Be("Of course you aren't.");
        sixthLink.TopicInfos[0].InvisibleContinue.Should().BeTrue();
        sixthLink.TopicInfos[0].Links.Should().ContainSingle();

        var seventhLink = sixthLink.TopicInfos[0].Links[0];
        seventhLink.TopicInfos.Should().ContainSingle();
        seventhLink.TopicInfos[0].Responses.Should().HaveCount(2);
        seventhLink.TopicInfos[0].Responses[0].FullResponse.Should()
            .Be("What brings you here, then, I wonder? Your story interests me.");
        seventhLink.TopicInfos[0].Responses[1].FullResponse.Should().Be("When you have a moment, I would love to hear it.");
        seventhLink.TopicInfos[0].InvisibleContinue.Should().BeFalse();
        seventhLink.TopicInfos[0].Links.Should().HaveCount(4);
    }
}
