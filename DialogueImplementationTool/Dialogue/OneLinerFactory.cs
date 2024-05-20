﻿using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
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
    }

    /*====================================================
       Post Processing
    ====================================================*/
    private static FormKey GetMainSpeaker(IDialogResponses responses) {
        foreach (var condition in responses.Conditions) {
            if (condition is not ConditionFloat { Data: FunctionConditionData data }) continue;

            if (data.Function == Condition.Function.GetIsID) return data.ParameterOneRecord.FormKey;
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

    public override void PreProcess(List<DialogueTopic> topics) { }

    protected static void PostProcess(IDialogTopic topic, PostProcessOptions options) {
        // ReorderBySpeaker(topic);
        if (options.RandomFlags) SetRandomFlags(topic, true);
        if (options.ResetHours > 0) SetResetHours(topic, options.ResetHours);
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
            if ((response.Flags.Flags & DialogResponses.Flag.SayOnce) != 0) continue;

            response.Flags.Flags |= DialogResponses.Flag.Random;

            if (addRandomEndFlag && (index + 1 >= topic.Responses.Count
                                     || GetMainSpeaker(topic.Responses[index + 1]) != GetMainSpeaker(response)))
                response.Flags.Flags |= DialogResponses.Flag.RandomEnd;
        }
    }

    protected sealed record PostProcessOptions(bool RandomFlags = false, float ResetHours = 0);
}
