using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class QuestSceneFactory(IDialogueContext context) : SceneFactory(context) {
    protected override Scene GetCurrentScene() {
        //Detect existing aliases
        foreach (var alias in Context.Quest.Aliases) {
            if (alias.UniqueActor.IsNull) continue;

            foreach (var speaker in AliasSpeakers.Where(speaker => speaker.FormKey == alias.UniqueActor.FormKey)) {
                speaker.AliasIndex = Convert.ToInt32(alias.ID);
            }
        }

        //Add missing aliases
        var addedAliases = new Dictionary<FormKey, QuestAlias>();
        foreach (var speaker in AliasSpeakers.Where(speaker => speaker.AliasIndex == -1)) {
            var alias = addedAliases.GetOrAdd(speaker.FormKey,
                () => {
                    speaker.AliasIndex = Context.Quest.Aliases.Count;
                    return GetAlias(speaker);
                });
            speaker.AliasIndex = Context.Quest.Aliases.Count;
            Context.Quest.Aliases.Add(alias);
        }

        //Add scene
        var scene = AddScene(
            Naming.GetFirstFreeIndex(
                i => $"{Context.Quest.EditorID}Scene_{i}",
                name => !Context.LinkCache.TryResolve<ISceneGetter>(name, out _),
                1),
            Context.Quest.FormKey);
        Context.AddScene(scene);

        return scene;
    }

    public override void PreProcessSpeakers() { }
}
