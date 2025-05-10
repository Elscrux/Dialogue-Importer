using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSuccessFailureSeparator {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestDialogueTopicCraneShore1() {
        // Import
        var (_, dialogue) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Check
        var illusionOption = dialogue.Topics[0].TopicInfos[0].Links[2];
        var persuadeOption = dialogue.Topics[0].TopicInfos[0].Links[3];
        persuadeOption.TopicInfos[0].Responses.Should().HaveCount(4);
        persuadeOption.TopicInfos[1].Responses.Should().HaveCount(3);

        // Process
        var successFailureSeparator = new SuccessFailureSeparator(new SkillCheckUtils(_testConstants.SkyrimDialogueContext));
        foreach (var link in dialogue.Topics.EnumerateLinks(true)) {
            successFailureSeparator.Process(link);
        }

        // Check
        illusionOption.TopicInfos[0].Responses.Should().ContainSingle();
        illusionOption.TopicInfos[0]
            .Responses[0]
            .FullResponse.Should()
            .Be("We can't be friends, but you're right, I didn't need to act so brash. I'm sorry. How can I help you?");
        illusionOption.TopicInfos[1].Responses.Should().ContainSingle();

        persuadeOption.TopicInfos[0].Responses.Should().HaveCount(4);
        persuadeOption.TopicInfos[0]
            .Responses[0]
            .FullResponse.Should()
            .Be(
                "Absolutely! We Nords have a proud history of settlement and statecraft. We share this ancestry with the native Roscreans.");
        persuadeOption.TopicInfos[1].Responses.Should().HaveCount(3);
        persuadeOption.TopicInfos[1]
            .Responses[0]
            .FullResponse.Should()
            .Be("Think? I know it could, but with you people like you around, things will never improve.");
    }
}
