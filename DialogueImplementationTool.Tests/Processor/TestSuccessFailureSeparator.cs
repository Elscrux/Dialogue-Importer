using DialogueImplementationTool.Dialogue.Processor;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSuccessFailureSeparator {
    [Fact]
    public void TestDialogueTopicCraneShore1() {
        var topic = TestDialogue.GetDialogueTopicCraneShore1();

        var illusionOption = topic.TopicInfos[0].Links[2];
        var persuadeOption = topic.TopicInfos[0].Links[3];
        persuadeOption.TopicInfos[0].Responses.Should().HaveCount(7);

        var successFailureSeparator = new SuccessFailureSeparator();
        foreach (var link in topic.EnumerateLinks()) {
            successFailureSeparator.Process(link);
        }

        illusionOption.TopicInfos[0].Responses.Should().ContainSingle();
        illusionOption.TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be("We can't be friends, but you're right, I didn't need to act so brash. I'm sorry. How can I help you?");
        illusionOption.TopicInfos[1].Responses.Should().ContainSingle();

        persuadeOption.TopicInfos[0].Responses.Should().HaveCount(4);
        persuadeOption.TopicInfos[0]
            .Responses[0]
            .Response.Should()
            .Be(
                "Absolutely! We Nords have a proud history of settlement and statecraft. We share this ancestry with the native Roscreans.");
        persuadeOption.TopicInfos[1].Responses.Should().HaveCount(3);
        persuadeOption.TopicInfos[1]
            .Responses[0]
            .Response.Should()
            .Be("Think? I know it could, but with you people like you around, things will never improve.");
    }
}
