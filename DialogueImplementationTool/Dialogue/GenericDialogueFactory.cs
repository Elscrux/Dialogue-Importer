using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class GenericDialogueFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    public override void GenerateDialogue(List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            foreach (var topicInfo in topic.TopicInfos) {
                var questFactory = GenericMetaData.GetGenericQuestFactory(topicInfo.MetaData);
                var dialogTopicFactory = GenericMetaData.GetGenericDialogTopicFactory(topicInfo.MetaData);
                var quest = questFactory.Create();
                var dialogTopic = dialogTopicFactory.Create(quest, topicInfo);
                topicInfo.Speaker ??= new NpcSpeaker(Context.LinkCache, new FormLinkInformation(FormKey.Null, typeof(INpcGetter)));
                var dialogTopicInfo = GetResponses(quest, topicInfo);
                dialogTopic.Responses.Add(dialogTopicInfo);
                var postProcessor = GenericMetaData.GetPostProcessor(topicInfo.MetaData);
                postProcessor.Process(quest, dialogTopic);
            }
        }
    }
}
