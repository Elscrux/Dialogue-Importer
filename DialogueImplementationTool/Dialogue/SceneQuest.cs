using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class QuestScene(IDialogueContext context) : SceneFactory(context) {
    protected override Scene GetCurrentScene(IQuest quest) {
        //Detect existing aliases
        foreach (var alias in quest.Aliases) {
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
                    speaker.AliasIndex = quest.Aliases.Count;
                    return GetAlias(speaker);
                });
            speaker.AliasIndex = quest.Aliases.Count;
            quest.Aliases.Add(alias);
        }

        //Add scene
        var scene = AddScene(
            Naming.GetFirstFreeIndex(
                i => $"{quest.EditorID}Scene_{i}",
                name => !Context.LinkCache.TryResolve<ISceneGetter>(name, out _),
                1),
            quest.FormKey);
        Context.AddScene(scene);

        return scene;
    }

    public override void PreProcessSpeakers() { }
}
