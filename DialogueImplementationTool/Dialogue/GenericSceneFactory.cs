using System.Collections.Generic;
using System.Windows;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
namespace DialogueImplementationTool.Dialogue;

public class GenericGenericSceneFactory(IDialogueContext context) : SceneFactory(context) {
    public override BaseDialogueFactory SpecifyType(List<DialogueTopic> topics) {
        // 3x3 scenes have a prompt set on their topic infos
        if (topics.Exists(t => t.TopicInfos.Exists(x => x.Prompt.FullText != string.Empty))) {
            return new GenericScene3x3Factory(Context);
        }

        return this;
    }

    public override void PreProcessSpeakers() {
        //Make sure there are only two speakers
        if (NameMappedSpeakers.Count != 2) MessageBox.Show("Error, there can only be 2 NPCs");
    }

    protected override Scene? GetCurrentScene() {
        if (AliasSpeakers is not [{} speaker1, {} speaker2]) return null;

        //Assign alias indices
        speaker1.AliasIndex = 2;
        speaker2.AliasIndex = 3;

        var npc1 = Context.LinkCache.Resolve<INpcGetter>(speaker1.FormKey);
        var npc2 = Context.LinkCache.Resolve<INpcGetter>(speaker2.FormKey);

        //Add quest
        var baseName = $"{Context.Quest.EditorID}Scene{npc1.GetName() + npc2.GetName()}";
        var questEditorId = Naming.GetFirstFreeIndex(
            i => baseName + i,
            name => !Context.LinkCache.TryResolve<IQuestGetter>(name, out _),
            1);
        var alias1 = GetEventAlias("Actor 1",
            [0x52, 0x31, 0x0, 0x0],
            AliasSpeakers[0].FormKey,
            AliasSpeakers[1].FormKey);
        alias1.ID = 0;
        var alias2 = GetEventAlias("Actor 2",
            [0x52, 0x32, 0x0, 0x0],
            AliasSpeakers[0].FormKey,
            AliasSpeakers[1].FormKey);
        alias2.ID = 1;
        const QuestAlias.Flag genericSceneAliasFlags =
            QuestAlias.Flag.AllowReserved | QuestAlias.Flag.AllowReuseInQuest;
        var alias3 = CreateAlias(AliasSpeakers[0]);
        alias3.Flags |= genericSceneAliasFlags;
        var alias4 = CreateAlias(AliasSpeakers[1]);
        alias4.Flags |= genericSceneAliasFlags;

        var sceneQuest = new Quest(Context.GetNextFormKey(), Context.Release) {
            EditorID = questEditorId,
            Priority = 10,
            Type = Quest.TypeEnum.None,
            Name = $"{Context.Quest.Name?.String} Scene {npc1.GetName()} {npc2.GetName()} {questEditorId[^1]}",
            Event = RecordTypes.ADIA,
            Aliases = [alias1, alias2, alias3, alias4],
        };
        Context.AddQuest(sceneQuest);

        //Add scene
        var scene = AddScene($"{questEditorId}Scene", sceneQuest.FormKey);
        scene.Flags = new Scene.Flag();
        scene.Flags |= Scene.Flag.BeginOnQuestStart | Scene.Flag.StopQuestOnEnd | Scene.Flag.Interruptable;
        Context.AddScene(scene);

        return scene;
    }
}
