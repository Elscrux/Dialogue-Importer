using DialogueImplementationTool.Dialogue.Topics;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSuccessFailureSeparator {
    [Fact]
    public void TestDialogueTopicCraneShore1() {
        var topic = TestDialogue.GetDialogueTopicCraneShore1();

        topic.TopicInfos[0].Links[3].TopicInfos[0].Responses.Count.Should().Be(7);

        var successFailureSeparator = new SuccessFailureSeparator();
        foreach (var link in topic.EnumerateLinks()) {
            successFailureSeparator.Process(link);
        }

        topic.TopicInfos[0].Links[2].TopicInfos[0].Responses.Count.Should().Be(1);
        topic.TopicInfos[0]
            .Links[2]
            .TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be("We can't be friends, but you're right, I didn't need to act so brash. I'm sorry. How can I help you?");
        topic.TopicInfos[0].Links[2].TopicInfos[1].Responses.Count.Should().Be(1);

        topic.TopicInfos[0].Links[3].TopicInfos[0].Responses.Count.Should().Be(4);
        topic.TopicInfos[0]
            .Links[3]
            .TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be(
                "Absolutely! We Nords have a proud history of settlement and statecraft. We share this ancestry with the native Roscreans.");
        topic.TopicInfos[0].Links[3].TopicInfos[1].Responses.Count.Should().Be(3);
        topic.TopicInfos[0]
            .Links[3]
            .TopicInfos[1]
            .Responses[0]
            .Response.Should()
            .Be("Think? I know it could, but with you people like you around, things will never improve.");
    }
}
