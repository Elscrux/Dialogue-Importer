using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class QuestSceneFactory(IDialogueContext context) : SceneFactory(context) {
    protected override (Scene? Scene, IQuest? QuestForDialogue) GetCurrentScene() {
        // Set alias indices
        foreach (var aliasSpeaker in AliasSpeakers) {
            var alias = Context.Quest.GetOrAddAlias(Context.LinkCache, aliasSpeaker.FormKey);
            aliasSpeaker.AliasIndex = Convert.ToInt32(alias.ID);
        }

        //Add scene
        var scene = AddScene(
            Naming.GetFirstFreeIndex(
                i => $"{Context.Quest.EditorID}Scene_{i}",
                name => !Context.LinkCache.TryResolve<ISceneGetter>(name, out _),
                1),
            Context.Quest.FormKey);
        Context.AddScene(scene);

        return (scene, Context.Quest);
    }

    public override void PreProcessSpeakers() {}
}
