using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class IdleFactory(IDialogueContext context) : OneLinerFactory(context) {
    private static readonly PostProcessOptions PostProcessOptions = new(true);
    private DialogTopic? _topic;

    public override void GenerateDialogue(List<DialogueTopic> topics) {
        var editorId = $"{Context.Quest.EditorID}Idles";

        _topic = Context.GetTopic(editorId) ?? new DialogTopic(Context.GetNextFormKey(), Context.Release) {
            EditorID = editorId,
            Name = editorId,
            Priority = 2500,
            Quest = new FormLinkNullable<IQuestGetter>(Context.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Misc,
            Subtype = DialogTopic.SubtypeEnum.Idle,
            SubtypeName = "IDLE",
        };

        GenerateDialogue(Context.Quest, topics, _topic);
    }

    public override void PostProcess() {
        if (_topic is not null) PostProcess(_topic, PostProcessOptions);
    }
}
