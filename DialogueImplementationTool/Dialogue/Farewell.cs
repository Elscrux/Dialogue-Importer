using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public sealed class Farewell : OneLinerFactory {
    private static readonly PostProcessOptions PostProcessOptions = new(true, 2);
    private static DialogTopic? _topic;
    
    public override void GenerateDialogue(List<DialogueTopic> topics) {
        _topic ??= new DialogTopic(Mod.GetNextFormKey(), Release) {
            EditorID = $"{DialogueImplementer.Quest.EditorID}Goodbyes",
            Name = $"{DialogueImplementer.Quest.EditorID}Goodbyes",
            Priority = 2500,
            Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Misc,
            Subtype = DialogTopic.SubtypeEnum.Goodbye,
            SubtypeName = "GBYE"
        };

        GenerateDialogue(topics, _topic);
    }

    public override void PostProcess() {
        if (_topic != null) PostProcess(_topic, PostProcessOptions);
    }
}