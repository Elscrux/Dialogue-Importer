using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class DialogueProcessor(IDialogueContext context, EmotionChecker emotionChecker) : IDialogueProcessor {
    // Runs during document parsing
    public List<IDialogueResponseProcessor> ResponseProcessors { get; } = [
        new InvalidStringFixer(),
        new ResponseNoteExtractor(),
        new EmptyBracesRemover(),
        new BackToDialogueRemover(),
        new ScriptNotesParser(),
        new Trimmer(),
    ];

    // Runs during document parsing
    public List<IDialogueTopicInfoProcessor> TopicInfoProcessors { get; } = [
        new SayOnceChecker(),
        new GoodbyeChecker(),
        new TopicInfoTrimmer(),
        new TopicInfoInvalidStringFixer(),
    ];

    // Runs after document parsing
    public List<IDialogueTopicProcessor> TopicProcessors { get; } = [
        new TopicInfoNoteExtractor(),
        new PlayerIsRaceChecker(),
        new SuccessFailureSeparator(context),
        new RandomChecker(),
    ];

    // Runs after document parsing
    public List<IDialogueTopicListProcessor> TopicListProcessors { get; } = [];

    // Runs at the very end
    public List<IConversationProcessor> ConversationProcessors { get; } = [
        new BackToOptionsLinker(),
        new KeywordLinker(),
        new CollapseNoteOnlyResponse(),
        new SameResponseChecker(),
        new SharedInfoConverter(),
        new CollapseEmptyInvisibleContinues(),
        new BlockingChecker(),
        new SetInvisibleContinuePrompt(),
        new MergeIdenticalTopics(),
        emotionChecker,
    ];

    public DialogueProcessor Clone() => new(context, emotionChecker);

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
