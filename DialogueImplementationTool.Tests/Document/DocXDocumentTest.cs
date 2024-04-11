using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Parser;
using FluentAssertions;
namespace DialogueImplementationTool.Tests.Document;

public sealed class DocXDocumentTest {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestImportScene() {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Document/Examples/[Locked] Brina Cross City Scenes.docx"),
            _testConstants.DialogueProcessor);

        var dialogueTopics = docXDocumentParser.ParseScene(0);

        // One topic with 6 topic infos with one response each
        dialogueTopics.Should().ContainSingle();
        dialogueTopics[0].TopicInfos.Should().HaveCount(6);
        foreach (var topicInfo in dialogueTopics[0].TopicInfos) {
            topicInfo.Responses.Should().ContainSingle();
        }
    }

    [Fact]
    public void TestProcessScene() {
        _testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker> {
            { "Ornev", new AliasSpeaker(_testConstants.Speaker1.FormKey, "Ornev") },
            { "Astav", new AliasSpeaker(_testConstants.Speaker2.FormKey, "Astav") },
        });

        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Document/Examples/[Locked] Brina Cross City Scenes.docx"),
            _testConstants.DialogueProcessor);

        var dialogueTopics = docXDocumentParser.ParseScene(0);
        List<GeneratedDialogue> generatedDialogue = [
            new GeneratedDialogue(_testConstants.SkyrimDialogueContext,
                DialogueType.GenericScene,
                dialogueTopics,
                _testConstants.Speaker1.FormKey),
        ];
        _testConstants.DialogueProcessor.Process(generatedDialogue);

        new DialogueImplementer(_testConstants.SkyrimDialogueContext).ImplementDialogue(generatedDialogue);

        _testConstants.Mod.Scenes.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.Should().HaveCount(6);
        foreach (var dialogTopic in _testConstants.Mod.DialogTopics) {
            dialogTopic.Responses.Should().ContainSingle();
        }
    }

    [Fact]
    public void TestProcessScene2() {
        _testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker> {
            { "Ornev", new AliasSpeaker(_testConstants.Speaker1.FormKey, "Ornev") },
            { "Astav", new AliasSpeaker(_testConstants.Speaker2.FormKey, "Astav") },
        });

        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Document/Examples/[Locked] Brina Cross City Scenes.docx"),
            _testConstants.DialogueProcessor);

        var dialogueTopics = docXDocumentParser.ParseScene(0);
        List<GeneratedDialogue> generatedDialogue = [
            new GeneratedDialogue(_testConstants.SkyrimDialogueContext,
                DialogueType.GenericScene,
                dialogueTopics,
                _testConstants.Speaker1.FormKey),
        ];
        _testConstants.DialogueProcessor.Process(generatedDialogue);

        new DialogueImplementer(_testConstants.SkyrimDialogueContext).ImplementDialogue(generatedDialogue);

        _testConstants.Mod.Scenes.Should().ContainSingle();
        _testConstants.Mod.DialogTopics.Should().HaveCount(6);
        foreach (var dialogTopic in _testConstants.Mod.DialogTopics) {
            dialogTopic.Responses.Should().ContainSingle();
        }
    }
}
