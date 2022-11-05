using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public class QuestScene : SceneFactory {
    private static int _questSceneCount = 1; 
    
    public override void GenerateDialogue(List<DialogueTopic> topics) {
        //Get all topics in order
        var allTopics = GetAllTopics(topics);
        
        if (OverrideQuest == null) {
            var questContext = DialogueImplementer.Environment.LinkCache.ResolveContext<IQuest, IQuestGetter>(DialogueImplementer.Quest.FormKey);
            OverrideQuest = questContext.GetOrAddAsOverride(Mod);
        }
        
        //Detect existing aliases
        foreach (var alias in OverrideQuest.Aliases) {
            if (alias.UniqueActor.IsNull) continue;

            foreach (var speaker in AliasSpeakers.Where(speaker => speaker.FormKey == alias.UniqueActor.FormKey)) {
                speaker.AliasIndex = Convert.ToInt32(alias.ID);
                break;
            }
        }
        
        //Add missing aliases
        foreach (var speaker in AliasSpeakers.Where(speaker => speaker.AliasIndex == -1)) {
            var newAlias = GetAlias(speaker);
            newAlias.ID = Convert.ToUInt32(OverrideQuest.Aliases.Count);
            speaker.AliasIndex = OverrideQuest.Aliases.Count;
            OverrideQuest.Aliases.Add(newAlias);
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