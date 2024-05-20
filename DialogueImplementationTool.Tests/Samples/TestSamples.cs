using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Tests.Samples;

public static class TestSamples {
    public static GeneratedDialogue GetDialogue(
        TestConstants testConstants,
        IDocumentParser documentParser,
        DialogueType type,
        int index) {
        return BaseDialogueFactory.PrepareDialogue(
                testConstants.SkyrimDialogueContext,
                testConstants.DialogueProcessor,
                documentParser,
                new DialogueSelection {
                    Speaker = testConstants.Speaker1.FormKey,
                    Selection = {
                        [type] = true,
                    },
                },
                index)
            .First();
    }

    public static GeneratedDialogue GetBrinaCrossScenes(TestConstants testConstants, int scene) {
        testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker> {
            { "Ornev", new AliasSpeaker(testConstants.Speaker1.FormKey, "Ornev") },
            { "Astav", new AliasSpeaker(testConstants.Speaker2.FormKey, "Astav") },
        });

        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/[Locked] Brina Cross City Scenes.docx"));

        return GetDialogue(testConstants, docXDocumentParser, DialogueType.GenericScene, scene);
    }

    public static (GeneratedDialogue Scene1, GeneratedDialogue Scene2)
        GetOldwallScenes(TestConstants testConstants) {
        testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker> {
            { "Jastara", new AliasSpeaker(testConstants.Speaker1.FormKey, "Jastara") },
            { "Godehard", new AliasSpeaker(testConstants.Speaker2.FormKey, "Godehard") },
        });

        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/[Locked] Oldwall Radiant Scenes.docx"));

        return (
            GetDialogue(testConstants, docXDocumentParser, DialogueType.GenericScene, 0),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.GenericScene, 1));
    }

    public static (GeneratedDialogue Greeting, GeneratedDialogue Dialogue, GeneratedDialogue Farewell)
        GetBrinaCrossDialogue(TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/[Locked] Relia Niveus dialogue.docx"));

        return (
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Greeting, 1),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 2),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Farewell, 3));
    }

    public static (GeneratedDialogue Greeting, GeneratedDialogue Dialogue) GetCraneShoreDialogue(
        TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/Hjaltan Sun-Screamer.docx"));

        return (
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Greeting, 0),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 1));
    }

    public static (GeneratedDialogue DialogueLink, GeneratedDialogue DialogueOptions1, GeneratedDialogue
        DialogueOptions2)
        GetStyleGuideDialogue(TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/StyleGuide.docx"));

        return (
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 0),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 1),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 2));
    }
}
