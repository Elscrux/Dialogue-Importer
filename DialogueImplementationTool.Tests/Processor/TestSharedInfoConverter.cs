using DialogueImplementationTool.Dialogue.Processor;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSharedInfoConverter {
    [Fact]
    public void TestDialogueTopicCraneShore1_WithoutPreProcessing() {
        var topic = TestDialogue.GetDialogueTopicCraneShore1();
        var generatedDialogue = TestDialogue.TopicAsGeneratedDialogue(topic);

        var persuadeOption = topic.TopicInfos[0].Links[3].TopicInfos[0];
        persuadeOption.Responses.Should().HaveCount(7);

        var sharedInfoConverter = new SharedInfoConverter();
        sharedInfoConverter.Process(generatedDialogue);

        persuadeOption.Responses.Should().HaveCount(3);
        persuadeOption.Links.Should().ContainSingle();
        persuadeOption.Links[0].TopicInfos.Should().ContainSingle();
        persuadeOption.Links[0].TopicInfos[0].Responses[0].Response.Should().Be("Anyway, why are we talking again?");
        persuadeOption.Links[0].TopicInfos[0].SharedInfo.Should().BeNull();
    }

    [Fact]
    public void TestRawDialogueTopicCraneShore1_WithPreProcessing() {
        var topic = TestDialogue.GetDialogueTopicCraneShore1();
        var generatedDialogue = TestDialogue.TopicAsGeneratedDialogue(topic);

        var persuadeOption = topic.TopicInfos[0].Links[3];
        persuadeOption.TopicInfos[0].Responses.Should().HaveCount(7);

        // do success separation first
        var successFailureSeparator = new SuccessFailureSeparator();
        foreach (var link in topic.EnumerateLinks()) {
            successFailureSeparator.Process(link);
        }

        var sharedInfoConverter = new SharedInfoConverter();
        sharedInfoConverter.Process(generatedDialogue);

        var persuadeFirstOption = persuadeOption.TopicInfos[0];
        persuadeFirstOption.Responses.Should().HaveCount(3);
        persuadeFirstOption.Links.Should().ContainSingle();
        persuadeFirstOption.Links[0].TopicInfos[0].SharedInfo.Should().BeNull();
        persuadeFirstOption.Links[0]
            .TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be("Anyway, why are we talking again?");

        var persuadeSecondOption = persuadeOption.TopicInfos[1];
        persuadeSecondOption.Responses.Should().HaveCount(2);
        persuadeSecondOption.Links.Should().ContainSingle();
        persuadeSecondOption.Links[0].TopicInfos[0].SharedInfo.Should().BeNull();
        persuadeSecondOption.Links[0]
            .TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be("Anyway, why are we talking again?");
    }
}
