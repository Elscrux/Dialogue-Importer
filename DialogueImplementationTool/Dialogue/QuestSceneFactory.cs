using System;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class QuestSceneFactory(IDialogueContext context) : SceneFactory(context) {
    protected override (Scene? Scene, IQuest? QuestForDialogue) GetCurrentScene() {
        // Set alias indices
        foreach (var aliasSpeaker in AliasSpeakers) {
            var alias = Context.Quest.GetOrAddAliasUniqueActor(Context.LinkCache, aliasSpeaker.FormLink.FormKey);
            aliasSpeaker.AliasIndex = Convert.ToInt32(alias.ID);
        }

        //Add scene
        var scene = AddScene(
            Naming.GetFirstFreeIndex(
                i => $"{Context.Quest.EditorID}Scene_{i}",
                name => !Context.LinkCache.TryResolve<ISceneGetter>(name, out _),
                1),
            Context.Quest.FormKey);
        Context.AddRecord(scene);

        return (scene, Context.Quest);
    }

    public override void PreProcessSpeakers() {}
}
