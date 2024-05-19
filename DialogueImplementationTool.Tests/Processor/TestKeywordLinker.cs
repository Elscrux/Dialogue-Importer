using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Parser;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestKeywordLinker {
    [Fact]
    public void TestStyleGuideLinks() {
        var topic = TestDialogue.GetDialogueTopicStyleGuideLinks();
        var generatedDialogue = TestDialogue.TopicAsGeneratedDialogue(topic);

        var keywordLinker = new KeywordLinker();
        keywordLinker.Process(generatedDialogue);

        generatedDialogue[0].Topics[0].TopicInfos[0].Responses[0].Response.Should().NotStartWith("[DONE]");

        // One link from the first link
        generatedDialogue[0].Topics[0].TopicInfos[0].Links[0].TopicInfos[0].Responses[0].Response.Should().NotEndWith("[merge to DONE above]");
        generatedDialogue[0].Topics[0].TopicInfos[0].Links[0].TopicInfos[0].Links.Should().ContainSingle();
        generatedDialogue[0].Topics[0].TopicInfos[0].Links[0].TopicInfos[0].InvisibleContinue.Should().BeTrue();

        // Another link from the second link
        generatedDialogue[0].Topics[0].TopicInfos[0].Links[1].TopicInfos[0].Responses[0].Response.Should().NotEndWith("[merge to DONE above]");
        generatedDialogue[0].Topics[0].TopicInfos[0].Links[1].TopicInfos[0].Links.Should().ContainSingle();
        generatedDialogue[0].Topics[0].TopicInfos[0].Links[0].TopicInfos[0].InvisibleContinue.Should().BeTrue();
    }

    [Fact]
    public void TestStyleGuideOptionsLinks() {
        var testConstants = new TestConstants();
        var topics = TestDialogue.GetDialogueTopicStyleGuideOptionsLinks();
        List<GeneratedDialogue> generatedDialogue = [
            new GeneratedDialogue(testConstants.SkyrimDialogueContext,
                DialogueType.Dialogue,
                topics,
                testConstants.Speaker1.FormKey),
        ];

        var keywordLinker = new KeywordLinker();
        keywordLinker.Process(generatedDialogue);

        generatedDialogue[0].Topics[0].TopicInfos[0].Responses[0].Response.Should().NotEndWith("[HERE]");

        generatedDialogue[0].Topics[1].TopicInfos[0].Responses[0].Response.Should().NotEndWith("[merge to HERE above]");
        generatedDialogue[0].Topics[1].TopicInfos[0].Links.Should().HaveCount(2);
        generatedDialogue[0].Topics[1].TopicInfos[0].InvisibleContinue.Should().BeFalse();
    }
}
