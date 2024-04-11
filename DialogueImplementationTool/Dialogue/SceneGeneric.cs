using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
namespace DialogueImplementationTool.Dialogue;

public sealed class GenericScene(IDialogueContext context) : SceneFactory(context) {
    protected override Scene? GetCurrentScene(IQuest quest) {
        if (AliasSpeakers is not [{ } speaker1, { } speaker2]) return null;

        //Assign alias indices
        speaker1.AliasIndex = 2;
        speaker2.AliasIndex = 3;

        var npc1 = Context.LinkCache.Resolve<INpcGetter>(speaker1.FormKey);
        var npc2 = Context.LinkCache.Resolve<INpcGetter>(speaker2.FormKey);

        //Add quest
        var baseName = $"{quest.EditorID}Scene{npc1.GetName() + npc2.GetName()}";
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
        var alias3 = GetAlias(AliasSpeakers[0]);
        alias3.Flags |= genericSceneAliasFlags;
        var alias4 = GetAlias(AliasSpeakers[1]);
        alias4.Flags |= genericSceneAliasFlags;

        var sceneQuest = new Quest(Context.GetNextFormKey(), Context.Release) {
            EditorID = questEditorId,
            Priority = 10,
            Type = Quest.TypeEnum.None,
            Name = $"{quest.Name?.String} Scene {npc1.GetName()} {npc2.GetName()} {questEditorId[^1]}",
            Event = RecordTypes.ADIA,
            Aliases = [alias1, alias2, alias3, alias4],
        };
        Context.AddQuest(sceneQuest);

        //Add scene
        var scene = AddScene($"{questEditorId}Scene", sceneQuest.FormKey);
        scene.Flags = new Scene.Flag();
        scene.Flags |= Scene.Flag.BeginOnQuestStart | Scene.Flag.StopOnQuestEnd | Scene.Flag.Interruptable;
        Context.AddScene(scene);

        return scene;
    }

    protected override List<DialogueTopic> TransformLines(List<DialogueTopic> topics) {
        // Use default implementation for simple dialogue
        if (!Is3x3Scene(topics)) return base.TransformLines(topics);

        // Use 3x3 dialogue implementation if there are any links
        foreach (var topic in topics) {
            topic.ConvertResponsesToTopicInfos();

            var speakerName = topic.GetPlayerText();
            var speaker = GetSpeaker(speakerName);

            foreach (var topicInfo in topic.TopicInfos) {
                topicInfo.Random = true;
                topicInfo.Speaker = speaker;
                topicInfo.Prompt = string.Empty;
            }
        }

        return [..topics];
    }

    protected override IReadOnlyList<AliasSpeaker> GetSpeakers(List<DialogueTopic> topics) {
        if (!Is3x3Scene(topics)) return base.GetSpeakers(topics);

        var speakerNames = topics.SelectMany(topic => topic.TopicInfos)
            .Select(x => x.Prompt)
            .Distinct();

        return Context.GetAliasSpeakers(speakerNames);
    }

    private static bool Is3x3Scene(List<DialogueTopic> topics) => topics.Count > 1;

    public override void PreProcessSpeakers() {
        //Make sure there are only two speakers
        if (NameMappedSpeakers.Count != 2) MessageBox.Show("Error, there can only be 2 NPCs");
    }
}
