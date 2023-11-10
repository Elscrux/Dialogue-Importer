using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public abstract class OneLinerFactory : DialogueFactory {
    public record PostProcessOptions(bool RandomFlags = false, float ResetHours = 0);

    private static bool _needPostProcessing;
    
    protected static void GenerateDialogue(List<DialogueTopic> topics, DialogTopic dialogTopic) {
        var topicsList = TopicsTreeToList(topics);
        
        var lastFormKey = FormKey.Null;
        if (dialogTopic.Responses.Count > 0) {
            lastFormKey = dialogTopic.Responses[^1].FormKey;
        }

        foreach (var topic in topicsList) {
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

    protected static void PostProcess(IDialogTopic topic, PostProcessOptions options) {
        if (!_needPostProcessing) return;

        // ReorderBySpeaker(topic);
        if (options.RandomFlags) SetRandomFlags(topic, true);
        if (options.ResetHours > 0) SetResetHours(topic, options.ResetHours);

        _needPostProcessing = false;
    }

    private static void SetResetHours(IDialogTopic topic, float resetHours) {
        foreach (var response in topic.Responses) {
            response.Flags ??= new DialogResponseFlags();
            response.Flags.ResetHours = resetHours;
        }
    }

    private static void SetRandomFlags(IDialogTopic topic, bool addRandomEndFlag) {
        for (var index = 0; index < topic.Responses.Count; index++) {
            var response = topic.Responses[index];
            response.Flags ??= new DialogResponseFlags();
            response.Flags.Flags |= DialogResponses.Flag.Random;

            if (addRandomEndFlag && (index + 1 >= topic.Responses.Count || GetMainSpeaker(topic.Responses[index + 1]) != GetMainSpeaker(response))) {
                response.Flags.Flags |= DialogResponses.Flag.RandomEnd;
            }
        }
    }
}
