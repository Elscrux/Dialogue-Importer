using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class Idle(IDialogueContext context) : OneLinerFactory(context) {
    private static readonly PostProcessOptions PostProcessOptions = new(true);
    private DialogTopic? _topic;

    public override void GenerateDialogue(IQuest quest, List<DialogueTopic> topics) {
        var editorId = $"{quest.EditorID}Idles";

        _topic = Context.GetTopic(editorId) ?? new DialogTopic(Context.GetNextFormKey(), Context.Release) {
            EditorID = editorId,
            Name = editorId,
            Priority = 2500,
            Quest = new FormLinkNullable<IQuestGetter>(quest.FormKey),
            Category = DialogTopic.CategoryEnum.Misc,
            Subtype = DialogTopic.SubtypeEnum.Idle,
            SubtypeName = "IDLE",
        };

        GenerateDialogue(quest, topics, _topic);
    }

    public override void PostProcess() {
        if (_topic is not null) PostProcess(_topic, PostProcessOptions);
    }
}
