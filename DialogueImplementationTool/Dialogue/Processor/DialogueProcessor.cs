using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class DialogueProcessor(EmotionChecker emotionChecker) : IDialogueProcessor {
    public List<IDialogueResponseProcessor> ResponseProcessors { get; } = [
        new InvalidStringFixer(),
        new NoteExtractor(),
        new EmptyBracesRemover(),
        new BackToDialogueRemover(),
        new ScriptNotesParser(),
        new Trimmer(),
    ];

    public List<IDialogueTopicInfoProcessor> TopicInfoProcessors { get; } = [
        new SayOnceChecker(),
        new GoodbyeChecker(),
        new TopicInfoTrimmer(),
        new TopicInfoInvalidStringFixer(),
        new PlayerIsRaceChecker(),
        emotionChecker,
    ];

    public List<IDialogueTopicProcessor> TopicProcessors { get; } = [
        new SuccessFailureSeparator(),
        new RandomChecker(),
    ];

    public List<IDialogueTopicListProcessor> TopicListProcessors { get; } = [];

    public List<IConversationProcessor> ConversationProcessors { get; } = [
        new BackToOptionsLinker(),
        new KeywordLinker(),
        new CollapseNoteOnlyResponse(),
        new SameResponseChecker(),
        new SharedInfoConverter(),
        new CollapseEmptyInvisibleContinues(),
        new BlockingChecker(),
        new MergeIdenticalTopics(),
    ];

    public DialogueProcessor Clone() => new(emotionChecker);

    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
        foreach (var processor in ResponseProcessors) {
            processor.Process(response, textSnippets);
        }
    }

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var processor in TopicInfoProcessors) {
            processor.Process(topicInfo);
        }
    }

    public void Process(DialogueTopic topic) {
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

    public void Process(Conversation conversation) {
        foreach (var processor in ConversationProcessors) {
            processor.Process(conversation);
        }
    }
}
