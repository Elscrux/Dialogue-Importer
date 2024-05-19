using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Tests.Factory;

public sealed class TestSceneFactory {
    private readonly TestConstants _testConstants = new();

    [Fact]
    public void TestPreprocessing() {
        _testConstants.SpeakerSelection = new InjectedSpeakerSelection(new Dictionary<string, AliasSpeaker> {
            { "Jastara", new AliasSpeaker(_testConstants.Speaker1.FormKey, "Jastara") },
            { "Godehard", new AliasSpeaker(_testConstants.Speaker2.FormKey, "Godehard") },
        });
        List<DialogueTopic> scene1 = [TestDialogue.GetSceneBranchesOldwallScene1()];
        List<DialogueTopic> scene2 = [TestDialogue.GetSceneBranchesOldwallScene2()];

        // Process shared infos
        _testConstants.DialogueProcessor.Process([
            new GeneratedDialogue(
                _testConstants.SkyrimDialogueContext,
                DialogueType.GenericScene,
                scene1,
                FormKey.Null),
            new GeneratedDialogue(
                _testConstants.SkyrimDialogueContext,
                DialogueType.GenericScene,
                scene2,
                FormKey.Null),
        ]);

        var genericSceneFactory = new GenericScene(_testConstants.SkyrimDialogueContext);

        scene1[0].TopicInfos.Should().HaveCount(7);
        scene2[0].TopicInfos.Should().HaveCount(6);

        genericSceneFactory.PreProcess(scene1);
        genericSceneFactory.PreProcess(scene2);

        // each combines two responses in one info
        scene1.Should().HaveCount(6);
        scene2.Should().HaveCount(6);
    }
}
