using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
using FluentAssertions;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Tests.Document;

public sealed class DocXDocumentTest {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestScene() {
        _testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker> {
            { "Ornev", new AliasSpeaker(_testConstants.Speaker1.FormKey, "Ornev") },
            { "Astav", new AliasSpeaker(_testConstants.Speaker2.FormKey, "Astav") },
        });

        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Document/Examples/[Locked] Brina Cross City Scenes.docx"),
            _testConstants.DialogueProcessor);

        var dialogueTopics = (docXDocumentParser as IDocumentParser).ParseScene(0);

        // Import
        dialogueTopics.Should().ContainSingle();
        dialogueTopics[0].TopicInfos.Should().HaveCount(6);

        // Process
        List<GeneratedDialogue> generatedDialogue = [
            new GeneratedDialogue(_testConstants.SkyrimDialogueContext,
                DialogueType.GenericScene,
                dialogueTopics,
                _testConstants.Speaker1.FormKey),
        ];
        _testConstants.DialogueProcessor.Process(generatedDialogue);

        var secondResponse = generatedDialogue[0].Topics[0].TopicInfos[1].Responses[0];
        secondResponse.Response.Should().NotContain("morose");
        secondResponse.ScriptNote.Should().Be("morose");

        // Implement
        new DialogueImplementer(_testConstants.SkyrimDialogueContext).ImplementDialogue(generatedDialogue);

        _testConstants.Mod.Scenes.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.Should().HaveCount(6);
        foreach (var dialogTopic in _testConstants.Mod.DialogTopics) {
            dialogTopic.Responses.Should().ContainSingle();
        }
    }

    [Fact]
    public void TestSceneWithBranches() {
        _testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker> {
            { "Jastara", new AliasSpeaker(_testConstants.Speaker1.FormKey, "Jastara") },
            { "Godehard", new AliasSpeaker(_testConstants.Speaker2.FormKey, "Godehard") },
        });

        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Document/Examples/[Locked] Oldwall Radiant Scenes.docx"),
            _testConstants.DialogueProcessor);

        var dialogueTopics = (docXDocumentParser as IDocumentParser).ParseScene(0)
            .Concat((docXDocumentParser as IDocumentParser).ParseScene(1))
            .ToList();

        // Import
        dialogueTopics.Should().HaveCount(2);
        dialogueTopics[0].TopicInfos.Should().HaveCount(8);
        dialogueTopics[1].TopicInfos.Should().HaveCount(8);

        // Process
        List<GeneratedDialogue> generatedDialogue = [
            new GeneratedDialogue(_testConstants.SkyrimDialogueContext,
                DialogueType.GenericScene,
                [dialogueTopics[0]],
                _testConstants.Speaker1.FormKey),
            new GeneratedDialogue(_testConstants.SkyrimDialogueContext,
                DialogueType.GenericScene,
                [dialogueTopics[1]],
                _testConstants.Speaker1.FormKey),
        ];
        _testConstants.DialogueProcessor.Process(generatedDialogue);

        generatedDialogue[0].Topics[0].TopicInfos.Should().HaveCount(8);
        generatedDialogue[1].Topics[0].TopicInfos.Should().HaveCount(8);

        // Implement
        new DialogueImplementer(_testConstants.SkyrimDialogueContext).ImplementDialogue(generatedDialogue);

        _testConstants.Mod.Scenes.Should().HaveCount(2);
        _testConstants.Mod.DialogTopics.Should().HaveCount(15); // 7 + 7 + 1 (shared dialogue)
    }

    [Fact]
    public void TestScene3x3() {
        _testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker> {
            { "Ornev", new AliasSpeaker(_testConstants.Speaker1.FormKey, "Ornev") },
            { "Claxter", new AliasSpeaker(_testConstants.Speaker2.FormKey, "Claxter") },
        });

        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Document/Examples/[Locked] Brina Cross City Scenes.docx"),
            _testConstants.DialogueProcessor);

        var dialogueTopics = (docXDocumentParser as IDocumentParser).ParseScene(2);

        // Import
        dialogueTopics.Should().HaveCount(3);
        dialogueTopics[0].TopicInfos.Should().HaveCount(2);
        dialogueTopics[1].TopicInfos.Should().HaveCount(2);
        dialogueTopics[2].TopicInfos.Should().HaveCount(3);

        // Process
        List<GeneratedDialogue> generatedDialogue = [
            new GeneratedDialogue(_testConstants.SkyrimDialogueContext,
                DialogueType.GenericScene,
                dialogueTopics,
                _testConstants.Speaker1.FormKey),
        ];
        _testConstants.DialogueProcessor.Process(generatedDialogue);

        var secondTopic = generatedDialogue[0].Topics[1].TopicInfos[0].Responses[0];
        secondTopic.Response.Should().NotContain("nervous");
        secondTopic.ScriptNote.Should().Be("nervous");

        // Implement
        new DialogueImplementer(_testConstants.SkyrimDialogueContext).ImplementDialogue(generatedDialogue);

        _testConstants.Mod.Scenes.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.Should().HaveCount(3);
        _testConstants.Mod.DialogTopics.First().Responses.Should().HaveCount(2);
        _testConstants.Mod.DialogTopics.Skip(1).First().Responses.Should().HaveCount(2);
        _testConstants.Mod.DialogTopics.Skip(2).First().Responses.Should().HaveCount(3);
        foreach (var dialogResponses in _testConstants.Mod.DialogTopics.SelectMany(topic => topic.Responses)) {
            dialogResponses.Flags.Should().NotBeNull();
            dialogResponses.Flags!.Flags.Should().HaveFlag(DialogResponses.Flag.Random);
        }
    }

    [Fact]
    public void TestGreeting() {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Document/Examples/[Locked] Relia Niveus dialogue.docx"),
            _testConstants.DialogueProcessor);

        var dialogueTopics = docXDocumentParser.ParseOneLiner(0);

        // Import
        dialogueTopics.Should().ContainSingle();
        dialogueTopics[0].TopicInfos.Should().HaveCount(2);
        foreach (var topicInfo in dialogueTopics[0].TopicInfos) {
            topicInfo.Responses.Should().ContainSingle();
        }

        // Process
        List<GeneratedDialogue> generatedDialogue = [
            new GeneratedDialogue(_testConstants.SkyrimDialogueContext,
                DialogueType.Greeting,
                dialogueTopics,
                _testConstants.Speaker1.FormKey),
        ];
        _testConstants.DialogueProcessor.Process(generatedDialogue);

        var secondResponse = generatedDialogue[0].Topics[0].TopicInfos[1].Responses[0];
        secondResponse.Response.Should().NotContain("Always happy to see a customer.");
        secondResponse.ScriptNote.Should().Be(string.Empty);

        // Implement
        new DialogueImplementer(_testConstants.SkyrimDialogueContext).ImplementDialogue(generatedDialogue);

        _testConstants.Mod.DialogTopics.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.First().Responses.Should().HaveCount(2);
    }


    [Fact]
    public void TestDialogue() {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Document/Examples/[Locked] Relia Niveus dialogue.docx"),
            _testConstants.DialogueProcessor);

        var dialogueTopics = docXDocumentParser.ParseDialogue(2);

        // Import
        dialogueTopics.Should().HaveCount(2);
        dialogueTopics[0].TopicInfos[0].Responses.Should().HaveCount(3);
        dialogueTopics[1].TopicInfos[0].Links.Should().HaveCount(2);

        // Process
        List<GeneratedDialogue> generatedDialogue = [
            new GeneratedDialogue(_testConstants.SkyrimDialogueContext,
                DialogueType.Dialogue,
                dialogueTopics,
                _testConstants.Speaker1.FormKey),
        ];
        _testConstants.DialogueProcessor.Process(generatedDialogue);

        generatedDialogue[0].Topics[0].TopicInfos[0].Responses[1].ScriptNote.Should().Be("emphasis: cosmopolitan");
        generatedDialogue[0].Topics[0].TopicInfos[0].Responses[2].Response.Should().NotContain("back to options");
        generatedDialogue[0].Topics[0].TopicInfos[0].Links.Should().BeEmpty();

        var links = generatedDialogue[0].Topics[1].TopicInfos[0].Links;
        links[0].TopicInfos[0].Links.Should().HaveCount(2);
        links[1].TopicInfos[0].Links.Should().BeEmpty();

        // Implement
        new DialogueImplementer(_testConstants.SkyrimDialogueContext).ImplementDialogue(generatedDialogue);

        _testConstants.Mod.DialogTopics.Should().HaveCount(4);
        _testConstants.Mod.DialogTopics.First().EditorID.Should().EndWith("1Topic");
        _testConstants.Mod.DialogTopics.Skip(1).First().EditorID.Should().EndWith("2Topic");
        _testConstants.Mod.DialogTopics.Skip(2).First().EditorID.Should().EndWith("2TopicA");
        _testConstants.Mod.DialogTopics.Skip(3).First().EditorID.Should().EndWith("2TopicB");
    }
}
