using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public abstract class OneLinerFactory : DialogueFactory {
    private static bool _needPostProcessing;
    
    protected static void GenerateDialogue(List<DialogueTopic> topics, DialogTopic dialogTopic) {
        var allTopics = GetAllTopics(topics);
        
        var lastFormKey = FormKey.Null;
        if (dialogTopic.Responses.Count > 0) {
            lastFormKey = dialogTopic.Responses[^1].FormKey;
        }

        foreach (var topic in allTopics) {
            var responses = GetResponses(topic, lastFormKey);
            lastFormKey = responses.FormKey;

            dialogTopic.Responses.Add(responses);
        }

        if (!Mod.DialogTopics.ContainsKey(dialogTopic.FormKey)) {
            Mod.DialogTopics.Add(dialogTopic);
        }
        
        _needPostProcessing = true;
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

    public override void PreProcess(List<DialogueTopic> topics) {}

    protected static void PostProcess(IDialogTopic topic) {
        if (!_needPostProcessing) return;
        
        // ReorderBySpeaker(topic);
        SetRandomFlags(topic, true);
        
        _needPostProcessing = false;
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
