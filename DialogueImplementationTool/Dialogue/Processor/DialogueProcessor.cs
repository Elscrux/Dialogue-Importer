using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public class DialogueProcessor(EmotionChecker emotionChecker) : IDialogueProcessor {
    public List<IDialogueResponseProcessor> ResponseProcessors { get; } = [
        new InvalidStringFixer(),
        new NoteExtractor(),
        new EmptyBracesRemover(),
        new BackToDialogueRemover(),
        new ScriptNotesParser(),
        new Trimmer(),
    ];

    public List<IDialogueTopicInfoProcessor> TopicInfoPostProcessors { get; } = [
        emotionChecker,
    ];

    public List<IDialogueTopicInfoProcessor> TopicInfoPreProcessors { get; } = [
        new SayOnceChecker(),
        new GoodbyeChecker(),
        new TopicInfoTrimmer(),
        new TopicInfoInvalidStringFixer(),
    ];

    public List<IDialogueTopicProcessor> TopicProcessors { get; } = [
        new SuccessFailureSeparator(),
        new RandomChecker(),
    ];

    public List<IDialogueProcessor> TopicListProcessors { get; } = [];

    public List<IConversationProcessor> ConversationProcessors { get; } = [
        new BackToOptionsLinker(),
        new KeywordLinker(),
        new SameResponseChecker(),
        new SharedInfoConverter(),
        new BlockingChecker(),
    ];

    public virtual void PreProcess(DialogueTopicInfo topicInfo) {
        foreach (var processor in TopicInfoPreProcessors) {
            processor.Process(topicInfo);
        }
    }

    public virtual void PostProcess(DialogueTopicInfo topicInfo) {
        foreach (var processor in TopicInfoPostProcessors) {
            processor.Process(topicInfo);
        }
    }

    public virtual void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
        foreach (var processor in ResponseProcessors) {
            processor.Process(response, textSnippets);
        }
    }

    public virtual void Process(DialogueTopic topic) {
        foreach (var processor in TopicProcessors) {
            foreach (var link in topic.EnumerateLinks(true)) {
                processor.Process(link);
            }
        }
    }

    public void Process(List<DialogueTopic> topics) {
        foreach (var processor in TopicListProcessors) {
            processor.Process(topics);
        }
    }

    public virtual void Process(Conversation conversation) {
        foreach (var processor in ConversationProcessors) {
            processor.Process(conversation);
        }
    }
}
