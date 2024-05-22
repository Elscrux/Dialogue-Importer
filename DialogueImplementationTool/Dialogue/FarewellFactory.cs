using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class FarewellFactory(IDialogueContext context) : OneLinerFactory(context) {
    public override IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) {
        dialogueProcessor.TopicInfoProcessors.Add(new RandomTopicInfo());
        dialogueProcessor.TopicInfoProcessors.Add(new ResetHourTopicInfo(2));
        return base.ConfigureProcessor(dialogueProcessor);
    }

    public override void GenerateDialogue(List<DialogueTopic> topics) {
        var editorId = $"{Context.Quest.EditorID}Goodbyes";

        var topic = Context.GetTopic(editorId) ?? new DialogTopic(Context.GetNextFormKey(), Context.Release) {
            EditorID = editorId,
            Name = editorId,
            Priority = 2500,
            Quest = new FormLinkNullable<IQuestGetter>(Context.Quest.FormKey),
            Category = DialogTopic.CategoryEnum.Misc,
            Subtype = DialogTopic.SubtypeEnum.Goodbye,
            SubtypeName = "GBYE",
        };

        GenerateDialogue(Context.Quest, topics, topic);
    }
}
