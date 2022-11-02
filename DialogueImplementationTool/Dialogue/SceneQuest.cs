using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public class QuestScene : SceneFactory {
    public override void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, string speakerName) {
        //Get speaker structures
        var uniqueSpeakers = GetSpeakers(topics);
        
        if (OverrideQuest == null) {
            var questContext = DialogueImplementer.Environment.LinkCache.ResolveContext<IQuest, IQuestGetter>(DialogueImplementer.Quest.FormKey);
            OverrideQuest = questContext.GetOrAddAsOverride(Mod);
        }
        
        //Add existing aliases
        foreach (var alias in OverrideQuest.Aliases) {
            if (alias.UniqueActor.IsNull) continue;

            foreach (var speaker in uniqueSpeakers.Where(speaker => speaker.FormKey == alias.UniqueActor.FormKey)) {
                speaker.AliasIndex = Convert.ToInt32(alias.ID);
                break;
            }
        }
        
        //Add new aliases
        foreach (var speaker in uniqueSpeakers.Where(speaker => speaker.AliasIndex == -1)) {
            var newAlias = GetAlias(speaker);
            newAlias.ID = Convert.ToUInt32(OverrideQuest.Aliases.Count);
            speaker.AliasIndex = OverrideQuest.Aliases.Count;
            OverrideQuest.Aliases.Add(newAlias);
        }
        
        //Get speakers mapped to name
        var nameMappedSpeakers = GetNameMappedSpeakers(uniqueSpeakers);

        //Add scene
        var scene = AddScene(
            $"{DialogueImplementer.Quest.EditorID}Scene_{SceneCount}",
            DialogueImplementer.Quest.FormKey,
            nameMappedSpeakers.Select(s => s.Value.AliasIndex).ToList()
        );
        Mod.Scenes.Add(scene);

        //Add lines
        AddLines(DialogueImplementer.Quest, scene, ParseLines(topics[0].Responses), nameMappedSpeakers);
    }
}