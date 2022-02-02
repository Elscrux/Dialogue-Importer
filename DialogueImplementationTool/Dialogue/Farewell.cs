using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public class Farewell : OneLinerFactory {
    private static DialogTopic? _topic;
    
    public override void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, string speakerName) {
        _topic ??= new DialogTopic(Mod.GetNextFormKey(), Release) {
            EditorID = $"{DialogueImplementer.Quest.EditorID}Goodbyes",
            Name = $"{DialogueImplementer.Quest.EditorID}Goodbyes",
            Priority = 2500,
            Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Misc,
            Subtype = DialogTopic.SubtypeEnum.Goodbye,
            SubtypeName = "GBYE"
        };

        GenerateDialogue(topics, speakerKey, _topic);
    }
}