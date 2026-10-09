using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestPlayerIsRaceChecker {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestDialogueTopicCraneShore1() {
        // Import
        var (greeting, _) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Check - conditions were added
        CheckCondition(greeting.Topics[0].TopicInfos[3].ExtraConditions[^2], Skyrim.Race.NordRace.FormKey);
        greeting.Topics[0].TopicInfos[3].ExtraConditions[^2].Flags.Should().HaveFlag(Condition.Flag.OR);
        CheckCondition(greeting.Topics[0].TopicInfos[3].ExtraConditions[^1], Skyrim.Race.NordRaceVampire.FormKey);

        // Check - note is gone
        greeting.Topics[0].TopicInfos[3].Responses[0]
            .StartNotes.Exists(x => x.Text == "if player is a Nord").Should().BeFalse();

        void CheckCondition(Condition condition, FormKey formKey) {
            var data = condition.Data.Should().BeOfType<GetPCIsRaceConditionData>();
            data.Subject.Race.Link.FormKey.Should().Be(formKey);
        }
    }

    [Fact]
    public void TestArgonianNotArgonianResponsesSplitIntoSeparateTopicInfos() {
        // Arrange - dialogue:
        // 1.1.1. [if PC is Argonian] Argonian Response [go to MERGE]
        // 1.1.2. [if PC is not Argonian] Not Argonian Response
        // 1.1.3. [MERGE] Merge Response
        var topic = new DialogueTopic {
            TopicInfos = [
                new DialogueTopicInfo {
                    Speaker = _testConstants.Speaker1,
                    Prompt = new DialogueText { Text = "Prompt" },
                    Responses = [
                        new DialogueResponse {
                            Response = "Argonian Response",
                            StartNotes = [new Note { Text = "if PC is Argonian" }],
                            EndsNotes = [new Note { Text = "go to MERGE" }],
                        },
                        new DialogueResponse {
                            Response = "Not Argonian Response",
                            StartNotes = [new Note { Text = "if PC is not Argonian" }],
                        },
                        new DialogueResponse {
                            Response = "Merge Response",
                            StartNotes = [new Note { Text = "MERGE" }],
                        },
                    ],
                },
            ],
        };

        // Act - process race conditions
        var playerIsRaceChecker = new PlayerIsRaceChecker();
         playerIsRaceChecker.Process(topic);

        // Act - process keyword links
        var factory = BaseDialogueFactory.GetBaseFactory(
            DialogueType.Dialogue,
            _testConstants.SkyrimDialogueContext);
        Conversation conversation = [new GeneratedDialogue(factory, [topic])];
        new KeywordLinker().Process(conversation);

        // Assert - argonian and not argonian responses are split into separate topic infos
        topic.TopicInfos.Should().HaveCount(2);

        var argonianInfo = topic.TopicInfos[0];
        argonianInfo.Responses.Should().ContainSingle();
        argonianInfo.Responses[0].Response.Should().Be("Argonian Response");
        argonianInfo.ExtraConditions.Should().HaveCount(2);
        CheckRaceCondition(argonianInfo.ExtraConditions[0], Skyrim.Race.ArgonianRace.FormKey, 1);
        argonianInfo.ExtraConditions[0].Flags.Should().HaveFlag(Condition.Flag.OR);
        CheckRaceCondition(argonianInfo.ExtraConditions[1], Skyrim.Race.ArgonianRaceVampire.FormKey, 1);
        argonianInfo.InvisibleContinue.Should().BeTrue();

        var notArgonianInfo = topic.TopicInfos[1];
        notArgonianInfo.Responses.Should().ContainSingle();
        notArgonianInfo.Responses[0].Response.Should().Be("Not Argonian Response");
        notArgonianInfo.ExtraConditions.Should().HaveCount(2);
        CheckRaceCondition(notArgonianInfo.ExtraConditions[0], Skyrim.Race.ArgonianRace.FormKey, 0);
        notArgonianInfo.ExtraConditions[0].Flags.Should().NotHaveFlag(Condition.Flag.OR);
        var conditionFloat = notArgonianInfo.ExtraConditions[0].Should().BeAssignableTo<IConditionFloatGetter>().Subject;
        conditionFloat.ComparisonValue.Should().Be(0.0f);
        CheckRaceCondition(notArgonianInfo.ExtraConditions[1], Skyrim.Race.ArgonianRaceVampire.FormKey, 0);
        conditionFloat = notArgonianInfo.ExtraConditions[1].Should().BeAssignableTo<IConditionFloatGetter>().Subject;
        conditionFloat.ComparisonValue.Should().Be(0.0f);
        notArgonianInfo.InvisibleContinue.Should().BeTrue();

        // Both topic infos link to the MERGE topic info
        argonianInfo.Links.Should().ContainSingle();
        notArgonianInfo.Links.Should().ContainSingle();
        argonianInfo.Links[0].Should().BeSameAs(notArgonianInfo.Links[0]);

        var mergeInfo = argonianInfo.Links[0].TopicInfos.Should().ContainSingle().Subject;
        mergeInfo.Responses.Should().ContainSingle();
        mergeInfo.Responses[0].Response.Should().Be("Merge Response");

        // All condition and keyword notes are gone
        topic.TopicInfos.SelectMany(x => x.AllNotes()).Should().BeEmpty();
        mergeInfo.AllNotes().Should().BeEmpty();

        void CheckRaceCondition(Condition condition, FormKey formKey, float comparisonValue) {
            var conditionFloat = condition.Should().BeOfType<ConditionFloat>().Subject;
            conditionFloat.ComparisonValue.Should().Be(comparisonValue);
            var data = conditionFloat.Data.Should().BeAssignableTo<IGetPCIsRaceConditionDataGetter>().Subject;
            data.Race.Link.FormKey.Should().Be(formKey);
        }
    }
}
