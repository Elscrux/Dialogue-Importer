using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class QuestScene(IDialogueContext context) : SceneFactory(context) {
    // todo remove static member
    private static int _questSceneCount = 1;

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
        // todo refactor with Naming.GetFirstFreeIndex
        var scene = AddScene(
            $"{quest.EditorID}Scene_{_questSceneCount}",
            quest.FormKey);
        Context.AddScene(scene);
        _questSceneCount++;

        return scene;
    }

    public override void PreProcessSpeakers() { }
}
