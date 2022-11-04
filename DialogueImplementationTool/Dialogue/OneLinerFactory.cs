using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Responses;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public abstract class OneLinerFactory : DialogueFactory {
    protected static void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, DialogTopic dialogTopic) {
        var lastFormKey = FormKey.Null;
        if (dialogTopic.Responses.Count > 0) {
            lastFormKey = dialogTopic.Responses[^1].FormKey;
        }

        for (var i = 0; i < topics[0].Responses.Count; i++) {
            var response = topics[0].Responses[i];

            var responses = GetResponses(
                speakerKey,
                new DialogueTopic { Responses = new List<DialogueResponse> { response } },
                lastFormKey);
            
            lastFormKey = responses.FormKey;

            dialogTopic.Responses.Add(responses);
        }

        if (!Mod.DialogTopics.ContainsKey(dialogTopic.FormKey)) {
            Mod.DialogTopics.Add(dialogTopic);
        }
    }

    /*====================================================
       Post Processing
    ====================================================*/
    private static FormKey GetMainSpeaker(IDialogResponses responses) {
        foreach (var condition in responses.Conditions) {
            if (condition is not ConditionFloat { Data: FunctionConditionData data }) continue;

            if (data.Function == Condition.Function.GetIsID) {
                return data.ParameterOneRecord.FormKey;
            }
        }

        return FormKey.Null;
    }

    private static void ReorderBySpeaker(IDialogTopic topic) {
        for (var index = 0; index < topic.Responses.Count; index++) {
            var currentSpeaker = GetMainSpeaker(topic.Responses[index]);

            var rightSpeaker = false;
            for (var runner = index + 1; runner < topic.Responses.Count; runner++) {
                var current = topic.Responses[runner];
                if (GetMainSpeaker(current) == currentSpeaker) {
                    if (rightSpeaker) break;

                    topic.Remove(current);
                    topic.Responses.Insert(index + 1, current);
                } else {
                    rightSpeaker = false;
                }
            }
        }
    }

    protected static void PostProcess(IDialogTopic topic) {
        // ReorderBySpeaker(topic);
        SetRandomFlags(topic, true);
    }

    private static void SetRandomFlags(IDialogTopic topic, bool addRandomEndFlag) {
        for (var index = 0; index < topic.Responses.Count; index++) {
            var response = topic.Responses[index];
            response.Flags ??= new DialogResponseFlags();
            response.Flags.Flags |= DialogResponses.Flag.Random;

            if (addRandomEndFlag) {
                if (index + 1 >= topic.Responses.Count || GetMainSpeaker(topic.Responses[index + 1]) != GetMainSpeaker(response)) {
                    response.Flags.Flags |= DialogResponses.Flag.RandomEnd;
                }
            }
        }
    }
}
