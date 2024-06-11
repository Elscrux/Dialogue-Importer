using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class DialogueProcessor : IDialogueProcessor {
    private readonly IDialogueContext _context;
    private readonly EmotionChecker _emotionChecker;

    public DialogueProcessor(IDialogueContext context, EmotionChecker emotionChecker) {
        _context = context;
        _emotionChecker = emotionChecker;

        ResponseProcessors = [
            new InvalidStringFixer(),
            new ResponseNoteExtractor(),
            new EmptyBracesRemover(),
            new BackToDialogueRemover(),
            new ScriptNotesParser(),
            new IdleChecker(_context),
            new Trimmer(),
        ];

        TopicInfoProcessors = [
            new DeadAliveChecker(_context),
            new TalkedToChecker(_context),
            new TimeChecker(),
            new SayOnceChecker(),
            new GoodbyeChecker(),
            new TopicInfoTrimmer(),
            new TopicInfoInvalidStringFixer(),
        ];

        TopicProcessors = [
            new TopicInfoNoteExtractor(),
            new PlayerIsRaceChecker(),
            new SuccessFailureSeparator(context),
            new RandomChecker(),
            new RemoveEmptyTopicInfos(),
        ];

        ConversationProcessors = [
            new BackToOptionsLinker(),
            new KeywordLinker(),
            new CollapseNoteOnlyResponse(),
            new SameResponseChecker(),
            new SharedInfoConverter(),
            new CollapseEmptyInvisibleContinues(),
            new BlockingChecker(),
            new SetInvisibleContinuePrompt(),
            new BeggarServiceChecker(),
            new RumorServiceChecker(),
            new TrainServiceChecker(),
            new RumorServiceChecker(),
            new VendorServiceChecker(),
            new RentRoomServiceChecker(),
            new RemoveRootOptionChecker(), // Needs to be before dialogue quest lock unlock processor
            emotionChecker,
        ];

        if (context.Quest.IsDialogueQuest()) {
            ConversationProcessors.Add(new DialogueQuestLockUnlockProcessor(context));
        }
    }

    // Runs during document parsing
    public List<IDialogueResponseProcessor> ResponseProcessors { get; }

    // Runs during document parsing
    public List<IDialogueTopicInfoProcessor> TopicInfoProcessors { get; }

    // Runs after document parsing
    public List<IDialogueTopicProcessor> TopicProcessors { get; }

    // Runs after document parsing
    public List<IDialogueTopicListProcessor> TopicListProcessors { get; } = [];

    // Runs at the very end
    public List<IConversationProcessor> ConversationProcessors { get; }

    public DialogueProcessor Clone() => new(_context, _emotionChecker);

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
