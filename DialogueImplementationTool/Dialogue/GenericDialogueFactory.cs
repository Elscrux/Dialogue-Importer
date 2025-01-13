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
                var questFactory = GenericMetaData.GetGenericQuestFactory(topicInfo.MetaData);
                var quest = questFactory.Create();
                var dialogTopic = GetTopic(topicInfo, quest);
                topicInfo.Speaker ??= new NpcSpeaker(Context.LinkCache, FormKey.Null);
                var dialogTopicInfo = GetResponses(quest, topicInfo);
                dialogTopic.Responses.Add(dialogTopicInfo);
            }
        }
    }

    private DialogTopic GetTopic(DialogueTopicInfo topicInfo, Quest quest) {
        var category = GenericMetaData.GetCategory(topicInfo.MetaData);
        var subtype = GenericMetaData.GetSubtype(topicInfo.MetaData);

        var matchingTopic = Context.LinkCache.PriorityOrder.WinningOverrides<IDialogTopicGetter>()
            .Where(t => t.Quest.FormKey == quest.FormKey)
            .FirstOrDefault(t => t.Category == category && t.Subtype == subtype);

        DialogTopic dialogTopic;
        if (matchingTopic is null) {
            dialogTopic = new DialogTopic(Context.GetNextFormKey(), Context.Release) {
                EditorID = null,
                Name = null,
                Quest = quest.ToNullableLink(),
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
