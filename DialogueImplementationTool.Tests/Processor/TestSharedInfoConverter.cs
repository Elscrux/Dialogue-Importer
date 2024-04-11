using DialogueImplementationTool.Dialogue.Conversation;
using DialogueImplementationTool.Dialogue.Topics;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSharedInfoConverter {
    [Fact]
    public void TestDialogueTopicCraneShore1_WithoutPreProcessing() {
        var topic = TestDialogue.GetDialogueTopicCraneShore1();
        var generatedDialogue = TestDialogue.TopicAsGeneratedDialogue(topic);

        topic.TopicInfos[0].Links[3].TopicInfos[0].Responses.Count.Should().Be(7);

        var sharedInfoConverter = new SharedInfoConverter();
        sharedInfoConverter.Process(generatedDialogue);

        topic.TopicInfos[0].Links[3].TopicInfos[0].Responses.Count.Should().Be(3);
        topic.TopicInfos[0].Links[3].TopicInfos[0].Links.Count.Should().Be(1);
        topic.TopicInfos[0].Links[3].TopicInfos[0].Links[0].TopicInfos.Count.Should().Be(1);
        topic.TopicInfos[0]
            .Links[3]
            .TopicInfos[0]
            .Links[0]
            .TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be("Anyway, why are we talking again?");
        topic.TopicInfos[0].Links[3].TopicInfos[0].Links[0].TopicInfos[0].SharedInfo.Should().NotBeNull();
    }

    [Fact]
    public void TestRawDialogueTopicCraneShore1_WithPreProcessing() {
        var topic = TestDialogue.GetDialogueTopicCraneShore1();
        var generatedDialogue = TestDialogue.TopicAsGeneratedDialogue(topic);

        topic.TopicInfos[0].Links[3].TopicInfos[0].Responses.Count.Should().Be(7);

        // do success separation first
        var successFailureSeparator = new SuccessFailureSeparator();
        foreach (var link in topic.EnumerateLinks()) {
            successFailureSeparator.Process(link);
        }

        var sharedInfoConverter = new SharedInfoConverter();
        sharedInfoConverter.Process(generatedDialogue);

        topic.TopicInfos[0].Links[3].TopicInfos[0].Responses.Count.Should().Be(3);
        topic.TopicInfos[0].Links[3].TopicInfos[0].Links.Count.Should().Be(1);
        topic.TopicInfos[0].Links[3].TopicInfos[0].Links[0].TopicInfos[0].SharedInfo.Should().NotBeNull();
        topic.TopicInfos[0]
            .Links[3]
            .TopicInfos[0]
            .Links[0]
            .TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be("Anyway, why are we talking again?");

        topic.TopicInfos[0].Links[3].TopicInfos[1].Responses.Count.Should().Be(2);
        topic.TopicInfos[0].Links[3].TopicInfos[1].Links.Count.Should().Be(1);
        topic.TopicInfos[0].Links[3].TopicInfos[1].Links[0].TopicInfos[0].SharedInfo.Should().NotBeNull();
        topic.TopicInfos[0]
            .Links[3]
            .TopicInfos[1]
            .Links[0]
            .TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be("Anyway, why are we talking again?");
    }
}
