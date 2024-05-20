using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSharedInfoConverter {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestDialogueTopicCraneShore1_WithoutPreProcessing() {
        // Import
        var (_, dialogue) = TestSamples.GetCraneShoreDialogue(_testConstants);

        // Check - success and failure were separated
        var persuadeOptions = dialogue.Topics[0].TopicInfos[0].Links[3];
        persuadeOptions.TopicInfos[0].Responses.Should().HaveCount(4);
        persuadeOptions.TopicInfos[1].Responses.Should().HaveCount(3);

        // Process
        Conversation conversation = [dialogue];
        var sharedInfoConverter = new SharedInfoConverter();
        sharedInfoConverter.Process(conversation);

        // Check - added shared infos
        var persuadeFirstOption = persuadeOptions.TopicInfos[0];
        persuadeFirstOption.Responses.Should().HaveCount(3);
        persuadeFirstOption.Links.Should().ContainSingle();
        persuadeFirstOption.Links[0].TopicInfos[0].SharedInfo.Should().BeNull();
        persuadeFirstOption.Links[0].TopicInfos[0]
            .Responses[0]
            .FullResponse.Should()
            .Be("Anyway, why are we talking again?");

        var persuadeSecondOption = persuadeOptions.TopicInfos[1];
        persuadeSecondOption.Responses.Should().HaveCount(2);
        persuadeSecondOption.Links.Should().ContainSingle();
        persuadeSecondOption.Links[0].TopicInfos[0].SharedInfo.Should().BeNull();
        persuadeSecondOption.Links[0].TopicInfos[0]
            .Responses[0]
            .FullResponse.Should()
            .Be("Anyway, why are we talking again?");
    }
}
