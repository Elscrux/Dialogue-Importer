using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue; 

public sealed class QuestScene : SceneFactory {
    private static int _questSceneCount = 1; 
    
    public override void GenerateDialogue(List<DialogueTopic> topics) {
        //Get all topics in order
        var allTopics = GetAllTopics(topics);

        //Detect existing aliases
        foreach (var alias in DialogueImplementer.OverrideQuest.Aliases) {
            if (alias.UniqueActor.IsNull) continue;

            foreach (var speaker in AliasSpeakers.Where(speaker => speaker.FormKey == alias.UniqueActor.FormKey)) {
                speaker.AliasIndex = Convert.ToInt32(alias.ID);
            }
        }

        //Add missing aliases
        var addedAliases = new Dictionary<FormKey, QuestAlias>();
        foreach (var speaker in AliasSpeakers.Where(speaker => speaker.AliasIndex == -1)) {
            var newAlias = addedAliases.GetOrAdd(speaker.FormKey, () => GetAlias(speaker));
            newAlias.ID = Convert.ToUInt32(DialogueImplementer.OverrideQuest.Aliases.Count);
            speaker.AliasIndex = DialogueImplementer.OverrideQuest.Aliases.Count;
            DialogueImplementer.OverrideQuest.Aliases.Add(newAlias);
        }

        //Add scene
        var scene = AddScene(
            $"{DialogueImplementer.Quest.EditorID}Scene_{_questSceneCount}",
            DialogueImplementer.Quest.FormKey);
        Mod.Scenes.Add(scene);
        _questSceneCount++;

        //Add lines
        AddLines(DialogueImplementer.Quest, scene, allTopics);
    }
    
    public override void PreProcessSpeakers() {}
}
