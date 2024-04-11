using DialogueImplementationTool.Dialogue.Conversation;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestSameResponseChecker {
    [Fact]
    public void TestDialogueTopicCraneShore1() {
        var topic = TestDialogue.GetDialogueTopicCraneShore1();
        var generatedDialogue = TestDialogue.TopicAsGeneratedDialogue(topic);

        topic.TopicInfos[0].Links[0].TopicInfos[0].Responses.Count.Should().Be(0);

        var sameResponseChecker = new SameResponseChecker();
        sameResponseChecker.Process(generatedDialogue);

        topic.TopicInfos[0].Links[0].TopicInfos[0].Responses.Count.Should().Be(1);
    }
}
