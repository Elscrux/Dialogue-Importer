using System.Collections.Generic;
using System.Windows;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue; 

public class GenericScene : SceneFactory {
    private static int _genericSceneCount = 1; 
    
    public override void GenerateDialogue(List<DialogueTopic> topics) {
        if (AliasSpeakers.Count != 2) return;
        
        //Get all topics in order
        var allTopics = GetAllTopics(topics);
        
        //Assign alias indices
        var first = AliasSpeakers[0].FormKey;
        foreach (var speaker in AliasSpeakers) {
            speaker.AliasIndex = speaker.FormKey == first ? 2 : 3;
        }

        //Add quest
        var questEditorID = $"{DialogueImplementer.Quest.EditorID}Scene{_genericSceneCount}";
        var alias1 = GetEventAlias("Actor 1", AliasSpeakers[0].FormKey, AliasSpeakers[1].FormKey);
        alias1.ID = 0;
        var alias2 = GetEventAlias("Actor 2", AliasSpeakers[0].FormKey, AliasSpeakers[1].FormKey);
        alias2.ID = 1;
        var alias3 = GetAlias(AliasSpeakers[0]);
        alias3.ID = 2;
        var alias4 = GetAlias(AliasSpeakers[1]);
        alias4.ID = 3;
        
        var quest = new Quest(Mod.GetNextFormKey(), Release) {
            EditorID = questEditorID,
            Priority = 10,
            Type = Quest.TypeEnum.None,
            Name = $"{DialogueImplementer.Quest.Name?.String} Scene {_genericSceneCount}",
            Event = "ADIA",
            Aliases = new ExtendedList<QuestAlias> { alias1, alias2, alias3, alias4 }
        };
        Mod.Quests.Add(quest);

        //Add scene
        var scene = AddScene($"{questEditorID}Scene", quest.FormKey);
        scene.Flags = new Scene.Flag();
        scene.Flags |= Scene.Flag.BeginOnQuestStart | Scene.Flag.StopOnQuestEnd | Scene.Flag.Interruptable;
        Mod.Scenes.Add(scene);
        _genericSceneCount++;

        //Add lines
        AddLines(quest, scene, allTopics);
    }
    
    public override void PreProcessSpeakers() {
        //Make sure there are only two speakers
        if (NameMappedSpeakers.Count != 2) MessageBox.Show("Error, there can only be 2 NPCs");
    }
}