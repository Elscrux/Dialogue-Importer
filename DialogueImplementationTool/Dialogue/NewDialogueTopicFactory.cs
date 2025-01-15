using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class NewDialogueTopicFactory(IDialogueContext context) : IGenericDialogueTopicFactory {
    public DialogTopic Create(IQuestGetter quest, DialogueTopicInfo topicInfo) {
        var category = GenericMetaData.GetCategory(topicInfo.MetaData);
        var subtype = GenericMetaData.GetSubtype(topicInfo.MetaData);

        var matchingTopic = context.LinkCache.PriorityOrder.WinningOverrides<IDialogTopicGetter>()
            .Where(t => t.Quest.FormKey == quest.FormKey)
            .FirstOrDefault(t => t.Category == category && t.Subtype == subtype);

        DialogTopic dialogTopic;
        if (matchingTopic is null) {
            dialogTopic = new DialogTopic(context.GetNextFormKey(), context.Release) {
                EditorID = null,
                Name = null,
                Quest = quest.ToNullableLink(),
                Category = category,
                Subtype = subtype,
                SubtypeName = subtype.ToRecordType(),
            };
            context.AddRecord(dialogTopic);
        } else {
            dialogTopic = context.GetTopic(matchingTopic.FormKey);
        }

        return dialogTopic;
    }
}
