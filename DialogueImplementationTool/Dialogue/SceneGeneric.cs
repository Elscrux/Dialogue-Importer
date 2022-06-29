using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue; 

public class GenericScene : SceneFactory {
    public override void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, string speakerName) {
        //Get speaker structures
        var uniqueSpeakers = GetSpeakers(topics);
        
        //Make sure there are only two speakers
        var formKeys = uniqueSpeakers.Distinct(s => s.FormKey).ToList();
        if (formKeys.Count != 2) {
            MessageBox.Show("Error, there can only be 2 NPCs");
            return;
        }
        
        //Assign alias indices
        var first = uniqueSpeakers[0].FormKey;
        foreach (var speaker in uniqueSpeakers) {
            speaker.AliasIndex = speaker.FormKey == first ? 2 : 3;
        }
        var nameMappedSpeakers = GetNameMappedSpeakers(uniqueSpeakers);

        //Add quest
        var questEditorID = $"{DialogueImplementer.Quest.EditorID}Scene{SceneCount}";
        var alias1 = GetEventAlias("Actor 1", uniqueSpeakers[0].FormKey, uniqueSpeakers[1].FormKey);
        alias1.ID = 0;
        var alias2 = GetEventAlias("Actor 2", uniqueSpeakers[0].FormKey, uniqueSpeakers[1].FormKey);
        alias2.ID = 1;
        var alias3 = GetAlias(uniqueSpeakers[0]);
        alias3.ID = 2;
        var alias4 = GetAlias(uniqueSpeakers[1]);
        alias4.ID = 3;
        
        var quest = new Quest(Mod.GetNextFormKey(), Release) {
            EditorID = questEditorID,
            Priority = 10,
            Type = Quest.TypeEnum.None,
            Name = $"{DialogueImplementer.Quest.Name?.String} Scene {SceneCount}",
            Event = "ADIA",
            Aliases = new ExtendedList<QuestAlias> { alias1, alias2, alias3, alias4 }
        };
        Mod.Quests.Add(quest);

        //Add scene
        var scene = AddScene($"{questEditorID}Scene", quest.FormKey, new List<int> {2, 3});
        scene.Flags |= Scene.Flag.BeginOnQuestStart | Scene.Flag.StopOnQuestEnd | Scene.Flag.Interruptable;
        Mod.Scenes.Add(scene);

        //Add lines
        AddLines(quest, scene, ParseLines(topics[0].Responses), nameMappedSpeakers);
    }
}