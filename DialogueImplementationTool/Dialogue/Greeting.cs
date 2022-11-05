using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public class Greeting : OneLinerFactory {
    private static DialogTopic? _topic;
    
    public override void GenerateDialogue(List<DialogueTopic> topics) {
        _topic ??= new DialogTopic(Mod.GetNextFormKey(), Release) {
            EditorID = $"{DialogueImplementer.Quest.EditorID}Hellos",
            Name = $"{DialogueImplementer.Quest.EditorID}Hellos",
            Priority = 2500,
            Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Misc,
            Subtype = DialogTopic.SubtypeEnum.Hello,
            SubtypeName = "HELO"
        };

        GenerateDialogue(topics, _topic);
    }

    public override void PostProcess() {
        if (_topic != null) PostProcess(_topic);
    }
}