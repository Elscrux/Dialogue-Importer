using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class GenericDialogueFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    public override void GenerateDialogue(List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            foreach (var topicInfo in topic.TopicInfos) {
                if (topicInfo.MetaData["GenericQuestFactory"] is not IGenericDialogueQuestFactory questFactory)
                    throw new InvalidOperationException("GenericQuestFactory is not set");

                var quest = questFactory.Create();
                var dialogTopic = GetTopic(topicInfo, quest);
                topicInfo.Speaker ??= new NpcSpeaker(Context.LinkCache, FormKey.Null);
                var dialogTopicInfo = GetResponses(quest, topicInfo);
                dialogTopic.Responses.Add(dialogTopicInfo);
            }
        }
    }

    private DialogTopic GetTopic(DialogueTopicInfo topicInfo, Quest voiceTypeQuest) {
        if (topicInfo.MetaData["Category"] is not DialogTopic.CategoryEnum category)
            throw new InvalidOperationException("Category is not set");
        if (topicInfo.MetaData["Subtype"] is not DialogTopic.SubtypeEnum subtype)
            throw new InvalidOperationException("Subtype is not set");

        var matchingTopic = Context.LinkCache.PriorityOrder.WinningOverrides<IDialogTopicGetter>()
            .Where(t => t.Quest.FormKey == voiceTypeQuest.FormKey)
            .FirstOrDefault(t => t.Category == category && t.Subtype == subtype);

        DialogTopic dialogTopic;
        if (matchingTopic is null) {
            dialogTopic = new DialogTopic(Context.GetNextFormKey(), Context.Release) {
                EditorID = null,
                Name = null,
                Quest = voiceTypeQuest.ToNullableLink(),
                Category = category,
                Subtype = subtype,
                SubtypeName = subtype.ToRecordType(),
            };
            Context.AddRecord(dialogTopic);
        } else {
            dialogTopic = Context.GetTopic(matchingTopic.FormKey);
        }

        return dialogTopic;
    }
}
