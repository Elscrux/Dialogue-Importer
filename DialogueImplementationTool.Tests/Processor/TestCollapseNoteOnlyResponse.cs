using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestCollapseNoteOnlyResponse {
    [Fact]
    public void TestProcess() {
        // Arrange
        var topicInfo = new DialogueTopicInfo {
            Prompt = "Hi, Player. How are you?",
            Responses = [
                new DialogueResponse { Response = "I'm good." },
                new DialogueResponse { StartNotes = [new Note { Text = "[back to options]" }] },
            ]
        };

        // Act
        var collapseNoteOnlyResponse = new CollapseNoteOnlyResponse();
        collapseNoteOnlyResponse.Process(topicInfo);

        // Assert
        topicInfo.Prompt.Should().Be("Hi, Player. How are you?");
        topicInfo.Responses.Should().HaveCount(1);
        topicInfo.Responses[0].Response.Should().Be("I'm good.");
        topicInfo.Responses[0].HasNote("[back to options]").Should().BeTrue();
    }
}
