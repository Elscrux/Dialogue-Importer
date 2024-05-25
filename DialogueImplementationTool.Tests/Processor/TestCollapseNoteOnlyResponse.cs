using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestCollapseNoteOnlyResponse {
    [Fact]
    public void TestProcessEnd() {
        // Arrange
        var topicInfo = new DialogueTopicInfo {
            Prompt = "Hi, Player. How are you?",
            Responses = [
                new DialogueResponse { Response = "I'm good." },
                new DialogueResponse { StartNotes = [new Note { Text = "back to options" }] },
            ],
        };

        // Act
        var collapseNoteOnlyResponse = new CollapseNoteOnlyResponse();
        collapseNoteOnlyResponse.Process(topicInfo);

        // Assert
        topicInfo.Prompt.Should().Be("Hi, Player. How are you?");
        topicInfo.Responses.Should().ContainSingle();
        topicInfo.Responses[0].Response.Should().Be("I'm good.");
        topicInfo.Responses[0].HasNote("back to options").Should().BeTrue();
    }

    [Fact]
    public void TestProcessStart() {
        // Arrange
        var topicInfo = new DialogueTopicInfo {
            Prompt = "Hi, Player. How are you?",
            Responses = [
                new DialogueResponse { StartNotes = [new Note { Text = "happy" }] },
                new DialogueResponse { StartNotes = [new Note { Text = "very happy" }] },
                new DialogueResponse { Response = "I'm good." },
            ],
        };

        // Act
        var collapseNoteOnlyResponse = new CollapseNoteOnlyResponse();
        collapseNoteOnlyResponse.Process(topicInfo);

        // Assert
        topicInfo.Prompt.Should().Be("Hi, Player. How are you?");
        topicInfo.Responses.Should().ContainSingle();
        topicInfo.Responses[0].Response.Should().Be("I'm good.");
        topicInfo.Responses[0].HasNote("happy").Should().BeTrue();
        topicInfo.Responses[0].HasNote("very happy").Should().BeTrue();
    }

    [Fact]
    public void TestProcessStartAndEnd() {
        // Arrange
        var topicInfo = new DialogueTopicInfo {
            Prompt = "Hi, Player. How are you?",
            Responses = [
                new DialogueResponse { StartNotes = [new Note { Text = "happy" }] },
                new DialogueResponse { StartNotes = [new Note { Text = "y" }] },
                new DialogueResponse { Response = "I'm good." },
                new DialogueResponse { StartNotes = [new Note { Text = "very happy" }] },
                new DialogueResponse { StartNotes = [new Note { Text = "x" }] },
            ],
        };

        // Act
        var collapseNoteOnlyResponse = new CollapseNoteOnlyResponse();
        collapseNoteOnlyResponse.Process(topicInfo);

        // Assert
        topicInfo.Prompt.Should().Be("Hi, Player. How are you?");
        topicInfo.Responses.Should().ContainSingle();
        topicInfo.Responses[0].Response.Should().Be("I'm good.");
        topicInfo.Responses[0].HasNote("happy").Should().BeTrue();
        topicInfo.Responses[0].HasNote("very happy").Should().BeTrue();
        topicInfo.Responses[0].HasNote("x").Should().BeTrue();
    }
}
