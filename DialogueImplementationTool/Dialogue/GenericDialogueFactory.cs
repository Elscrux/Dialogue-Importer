using System.Collections.Generic;
using System.Linq;
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
                if (dialogTopic.Responses.Any(r => {
                    return topicInfo.ExtraConditions.All(condition => r.Conditions.Contains(condition))
                     && topicInfo.Goodbye == (r.Flags is not null && r.Flags.Flags.HasFlag(DialogResponses.Flag.Goodbye))
                     && topicInfo.InvisibleContinue
                     == (r.Flags is not null && r.Flags.Flags.HasFlag(DialogResponses.Flag.InvisibleContinue))
                     && topicInfo.SayOnce == (r.Flags is not null && r.Flags.Flags.HasFlag(DialogResponses.Flag.SayOnce))
                     && topicInfo.Random == (r.Flags is not null && r.Flags.Flags.HasFlag(DialogResponses.Flag.Random))
                     && topicInfo.Responses.Select(x => x.Response).SequenceEqual(r.Responses.Select(x => x.Text.String));
                })) {
                    continue;
                }

                var dialogTopicInfo = GetResponses(quest, topicInfo);
                dialogTopic.Responses.Add(dialogTopicInfo);
                var postProcessor = GenericMetaData.GetPostProcessor(topicInfo.MetaData);
                postProcessor.Process(quest, dialogTopic);
            }
        }
    }
}
