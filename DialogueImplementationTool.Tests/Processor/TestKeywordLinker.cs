using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Tests.Samples;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestKeywordLinker {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestStyleGuideLinks() {
        // Import
        var (dialogueLink, _, _) = TestSamples.GetStyleGuideDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogueLink];
        var keywordLinker = new KeywordLinker();
        keywordLinker.Process(conversation);

        // Check
        conversation[0].Topics[0].TopicInfos[0].Responses[0].FullResponse.Should().NotStartWith("[DONE]");

        // One link from the second link
        var secondResponse = conversation[0].Topics[1].TopicInfos[0];
        secondResponse.Responses[0].FullResponse.Should().NotEndWith("[merge to DONE above]");
        secondResponse.Links.Should().ContainSingle();
        secondResponse.InvisibleContinue.Should().BeTrue();

        // Another link from the third link
        var thirdResponse = conversation[0].Topics[2].TopicInfos[0];
        thirdResponse.Responses[0].FullResponse.Should().NotEndWith("[merge to DONE2 above]");
        thirdResponse.Links.Should().ContainSingle();
        thirdResponse.InvisibleContinue.Should().BeTrue();
    }

    [Fact]
    public void TestStyleGuideOptionsLinks() {
        // Import
        var (_, dialogueOptions1, dialogueOptions2) = TestSamples.GetStyleGuideDialogue(_testConstants);

        // Process
        Conversation conversation = [dialogueOptions1, dialogueOptions2];
        var keywordLinker = new KeywordLinker();
        keywordLinker.Process(conversation);

        // Check
        conversation[0].Topics[0].TopicInfos[0].Responses[0].FullResponse.Should().NotEndWith("[HERE]");

        var dialogueTopicInfo = conversation[1].Topics[0].TopicInfos[0];
        dialogueTopicInfo.Responses[0].FullResponse.Should().NotEndWith("[merge to HERE above]");
        dialogueTopicInfo.Links.Should().HaveCount(2);
        dialogueTopicInfo.InvisibleContinue.Should().BeFalse();
    }
}
