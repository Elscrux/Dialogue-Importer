﻿using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
namespace DialogueImplementationTool.Tests.Samples;

public static class TestSamples {
    public static GeneratedDialogue GetDialogue(
        TestConstants testConstants,
        IDocumentIterator documentIterator,
        DialogueType type,
        int index) {
        return documentIterator.ParseDialogue(
                testConstants.SkyrimDialogueContext,
                testConstants.DialogueProcessor,
                new DialogueSelection {
                    Speaker = testConstants.Speaker1.FormLink,
                    SelectedTypes = { type },
                },
                index)
            .First();
    }

    public static GeneratedDialogue GetBrinaCrossScenes(TestConstants testConstants, int scene) {
        testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, ISpeaker> {
            { "Ornev", new AliasSpeaker(testConstants.Speaker1.FormLink, "Ornev") },
            { "Astav", new AliasSpeaker(testConstants.Speaker2.FormLink, "Astav") },
        });

        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/[Locked] Brina Cross City Scenes.docx"));

        return GetDialogue(testConstants, docXDocumentParser, DialogueType.GenericScene, scene);
    }

    public static (GeneratedDialogue Scene1, GeneratedDialogue Scene2)
        GetOldwallScenes(TestConstants testConstants) {
        testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, ISpeaker> {
            { "Jastara", new AliasSpeaker(testConstants.Speaker1.FormLink, "Jastara") },
            { "Godehard", new AliasSpeaker(testConstants.Speaker2.FormLink, "Godehard") },
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

    public static (GeneratedDialogue Greeting, GeneratedDialogue Dialogue, GeneratedDialogue Farewell) GetIdonaVerusDialogue(
        TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/Idona Verus.docx"));

        return (
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Greeting, 0),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 1),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Farewell, 2));
    }

    public static (GeneratedDialogue Greeting, GeneratedDialogue Farewell, GeneratedDialogue Dialogue, GeneratedDialogue
        Dialogue2)
        GetMalwonDialogue(TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/[Locked] Malwon.docx"));

        return (
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Greeting, 0),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Farewell, 1),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 2),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 3));
    }

    public static (GeneratedDialogue Greeting, GeneratedDialogue Dialogue, GeneratedDialogue Dialogue2, GeneratedDialogue
        Farewell)
        GetAdilaNadeDialogue(TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/Adila Nade.docx"));

        return (
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Greeting, 0),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 1),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 2),
            GetDialogue(testConstants, docXDocumentParser, DialogueType.Farewell, 3));
    }

    public static GeneratedDialogue GetMultiLevelConditionDialogue(TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/MultiLevelCondition.docx"));

        return GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 0);
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

    public static GeneratedDialogue
        GetLockTestDialogue(TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/LockTest.docx"));

        return GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 0);
    }

    public static GeneratedDialogue
        GetMediAtMuhayDialogue(TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/[Locked] Medi at-Muhay [standard dialogue].docx"));

        return GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 2);
    }

    public static GeneratedDialogue
        GetMaenlornDialogue(TestConstants testConstants) {
        var docXDocumentParser = new DocXDocumentParser(
            Path.GetFullPath("../../../Samples/Documents/[Locked] Maenlorn [dialogue].docx"));

        return GetDialogue(testConstants, docXDocumentParser, DialogueType.Dialogue, 2);
    }
}
