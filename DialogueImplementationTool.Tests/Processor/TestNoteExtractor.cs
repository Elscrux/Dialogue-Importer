using System.Drawing;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Parser;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Processor;

public sealed class TestNoteExtractor {
    [Fact]
    public void TestStartNotes() {
        const string text = "[excited] Yes!";
        var dialogueResponse = new DialogueResponse { Response = text };

        var noteExtractor = new NoteExtractor();
        noteExtractor.Process(dialogueResponse, [new FormattedText(text, false, Color.Black)]);

        // Check
        dialogueResponse.Response.Should().Be("Yes!");
        dialogueResponse.StartNotes.Should().ContainSingle();
        dialogueResponse.StartNotes[0].Text.Should().Be("excited");
        dialogueResponse.EndsNotes.Should().BeEmpty();
    }

    [Fact]
    public void TestEndNotes() {
        const string text = "Yes! [back to options]";
        var dialogueResponse = new DialogueResponse { Response = text };

        var noteExtractor = new NoteExtractor();
        noteExtractor.Process(dialogueResponse, [new FormattedText(text, false, Color.Black)]);

        // Check
        dialogueResponse.Response.Should().Be("Yes!");
        dialogueResponse.StartNotes.Should().BeEmpty();
        dialogueResponse.EndsNotes.Should().ContainSingle();
        dialogueResponse.EndsNotes[0].Text.Should().Be("back to options");
    }

    [Fact]
    public void TestStartAndEndNotes() {
        const string text = "[excited] Yes! [back to options]";
        var dialogueResponse = new DialogueResponse { Response = text };

        var noteExtractor = new NoteExtractor();
        noteExtractor.Process(dialogueResponse, [new FormattedText(text, false, Color.Black)]);

        // Check
        dialogueResponse.Response.Should().Be("Yes!");
        dialogueResponse.StartNotes.Should().ContainSingle();
        dialogueResponse.StartNotes[0].Text.Should().Be("excited");
        dialogueResponse.EndsNotes.Should().ContainSingle();
        dialogueResponse.EndsNotes[0].Text.Should().Be("back to options");
    }

    [Fact]
    public void TestMultiNotes() {
        const string text = "Yes! [ back to options , unlock CONFIRM ]";
        var dialogueResponse = new DialogueResponse { Response = text };

        var noteExtractor = new NoteExtractor();
        noteExtractor.Process(dialogueResponse, [new FormattedText(text, false, Color.Black)]);

        // Check
        dialogueResponse.Response.Should().Be("Yes!");
        dialogueResponse.StartNotes.Should().BeEmpty();
        dialogueResponse.EndsNotes.Should().HaveCount(2);
        dialogueResponse.EndsNotes[0].Text.Should().Be("back to options");
        dialogueResponse.EndsNotes[1].Text.Should().Be("unlock CONFIRM");
    }
}
