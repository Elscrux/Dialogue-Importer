using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class DialogueProcessor(EmotionChecker emotionChecker) {
    private readonly IConversationProcessor[] _conversationProcessors = [
        new SameResponseChecker(),
        new SharedInfoConverter(),
        new BlockingChecker(),
        new BackToOptionsLinker(),
        new KeywordLinker(),
    ];

    private readonly IEnumerable<IDialogueTopicInfoProcessor> _topicInfoPostProcessors = [
        emotionChecker,
    ];

    private readonly IEnumerable<IDialogueTopicInfoProcessor> _topicInfoPreProcessors = [
        new SayOnceChecker(),
        new GoodbyeChecker(),
        new TopicInfoTrimmer(),
        new TopicInfoInvalidStringFixer(),
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
