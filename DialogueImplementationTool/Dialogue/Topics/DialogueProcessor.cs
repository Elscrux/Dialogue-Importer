﻿using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Conversation;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class DialogueProcessor(EmotionChecker emotionChecker) {
    private readonly IConversationProcessor[] _conversationProcessors = [
        new SameResponseChecker(),
        new SharedInfoConverter(),
        new BlockingChecker(),
        new BackToOptionsLinker(),
    ];

    private readonly IEnumerable<IDialogueTopicInfoProcessor> _topicInfoPostProcessors = [
        emotionChecker,
    ];

    private readonly IEnumerable<IDialogueTopicInfoProcessor> _topicInfoPreProcessors = [
        new SayOnceChecker(),
        new GoodbyeChecker(),
        new Trimmer(),
        new InvalidStringFixer(),
    ];

    private readonly IEnumerable<IDialogueTopicProcessor> _topicProcessors = [
        new SuccessFailureSeparator(),
        new RandomChecker(),
    ];

    public void PreProcess(DialogueTopicInfo topicInfo) {
        foreach (var preProcessor in _topicInfoPreProcessors) {
            preProcessor.Process(topicInfo);
        }
    }

    public void PostProcess(DialogueTopicInfo topicInfo) {
        foreach (var postProcessor in _topicInfoPostProcessors) {
            postProcessor.Process(topicInfo);
        }
    }

    public void Process(DialogueTopic topic) {
        foreach (var postProcessor in _topicProcessors) {
            foreach (var link in topic.EnumerateLinks()) {
                postProcessor.Process(link);
            }
        }
    }

    public void Process(List<GeneratedDialogue> dialogue) {
        foreach (var processor in _conversationProcessors) {
            processor.Process(dialogue);
        }
    }
}
