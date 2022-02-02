using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public class Greeting : OneLinerFactory {
    private static DialogTopic? _topic;
    
    public override void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, string speakerName) {
        _topic ??= new DialogTopic(Mod.GetNextFormKey(), Release) {
            EditorID = $"{DialogueImplementer.Quest.EditorID}Hellos",
            Name = $"{DialogueImplementer.Quest.EditorID}Hellos",
            Priority = 2500,
            Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Misc,
            Subtype = DialogTopic.SubtypeEnum.Hello,
            SubtypeName = "HELO"
        };

        GenerateDialogue(topics, speakerKey, _topic);
    }
}