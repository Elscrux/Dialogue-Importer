using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public abstract class OneLinerFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    protected void GenerateDialogue(IQuest quest, List<DialogueTopic> topics, DialogTopic dialogTopic) {
        var topicsInfoList = topics.ToTopicInfoList();

        var lastFormKey = FormKey.Null;
        if (dialogTopic.Responses.Count > 0) lastFormKey = dialogTopic.Responses[^1].FormKey;

        foreach (var topicInfo in topicsInfoList) {
            var responses = GetResponses(quest, topicInfo, lastFormKey);
            lastFormKey = responses.FormKey;
            dialogTopic.Responses.Add(responses);
        }

        Context.AddDialogTopic(dialogTopic);

        ReorderBySpeaker(dialogTopic);
    }

    private void ReorderBySpeaker(IDialogTopic topic) {
        // Gather responses per speaker
        var topicsWithSpeakers = topic.Responses
            .GroupBy(x => x.GetMainSpeaker())
            .OrderBy(x => Context.LinkCache.TryResolveIdentifier<INpcGetter>(x.Key, out var editorId) ? editorId : null)
            .ToArray();

        topic.Responses.Clear();

        // Insert responses ordered by speaker and say once
        IDialogResponses? lastResponses = null;
        foreach (var responsesGrouping in topicsWithSpeakers) {
            foreach (var responses in responsesGrouping.OrderBy(x => !x.IsSayOnce())) {
                if (lastResponses is not null) responses.PreviousDialog = lastResponses.ToNullableLink();
                lastResponses = responses;
                topic.Responses.Add(responses);
            }
        }

        // Set last responses
        var lastResponse = FormKey.Null;
        foreach (var response in topic.Responses) {
            response.PreviousDialog = new FormLinkNullable<IDialogResponsesGetter>(lastResponse);
            lastResponse = response.FormKey;
        }
    }

    public override void PreProcess(List<DialogueTopic> topics) {}
}
