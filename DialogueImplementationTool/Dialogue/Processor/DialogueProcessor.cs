using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class DialogueProcessor : IDialogueProcessor {
    private readonly IDialogueContext _context;
    private readonly IEmotionClassifierProvider _emotionClassifierProvider;

    public DialogueProcessor(IDialogueContext context, IEmotionClassifierProvider emotionClassifierProvider) {
        _context = context;
        _emotionClassifierProvider = emotionClassifierProvider;

        GenericDialogueProcessors = [
            new GenericLineInvalidStringFixer(),
            new GenericLineTrimmer(),
            new PlayerRaceProcessor(),
            new PlayerSexProcessor(),
            new RandomProcessor(),
            new TimeProcessor(),
            new WeatherProcessor(),
            new AcceptYieldProcessor(),
            new AimBowProcessor(),
            new AlertIdleProcessor(),
            new AlertToCombatProcessor(),
            new AlertToNormalProcessor(),
            new AssaultProcessor(),
            new AttackProcessor(),
            new BashShieldProcessor(),
            new BeingHitProcessor(),
            new BleedingOutProcessor(),
            new CollideActorProcessor(),
            new CollideItemProcessor(),
            new CombatToLostProcessor(),
            new CombatToNormalProcessor(),
            new CustomProcessor(_context),
            new DetectFriendDieProcessor(),
            new DyingProcessor(),
            new FleeProcessor(),
            new GoodbyeProcessor(),
            new GrabItemProcessor(),
            new GuardPursueProcessor(),
            new HelloProcessor(context),
            new IdleProcessor(),
            new LookAtLockedObjectProcessor(),
            new LostIdleProcessor(),
            new LostToCombatProcessor(),
            new LostToNormalProcessor(),
            new MurderProcessor(),
            new NormalToAlertProcessor(),
            new NormalToCombatProcessor(),
            new NoticeCorpseProcessor(),
            new ObserveCombatProcessor(),
            new PickpocketProcessor(),
            new PowerAttackProcessor(),
            new SceneProcessor(_context),
            new ShootBowNonCombatProcessor(),
            new ShoutingProcessor(),
            new StealingProcessor(),
            new TauntProcessor(),
            new TransformWerewolfProcessor(),
            new TrespassingProcessor(),
            new UseMeleeNonCombatProcessor(),
        ];

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
            new ActivePassiveChecker(),
            new TalkedToChecker(_context),
            new OffersServicesChecker(),
            new TimeChecker(),
            new SayOnceChecker(),
            new DispositionChecker(),
            new GoldChecker(),
            new GoodbyeChecker(),
            new TopicInfoTrimmer(),
            new TopicInfoInvalidStringFixer(),
        ];

        TopicProcessors = [
            new TopicInfoNoteExtractor(),
            new PlayerIsSexChecker(),
            new PlayerIsRaceChecker(),
            new PlayerIsVampireChecker(),
            new PlayerIsWerewolfChecker(),
            new SuccessFailureSeparator(context),
            new DeadAliveChecker(_context),
            new RandomChecker(),
            new BelongsToPreviousChecker(),
            new RemoveEmptyTopicInfos(),
        ];

        TopicListProcessors = [];

        ConversationProcessors = [
            new BackToOptionsLinker(),
            new CollapseNoteOnlyResponse(), // Needs to be before KeywordLinker
            new KeywordLinker(),
            new SameResponseChecker(),
            new SharedInfoConverter(),
            new CollapseEmptyInvisibleContinues(),
            new SetInvisibleContinuePrompt(),
            new BeggarServiceChecker(),
            new RumorServiceChecker(),
            new TrainServiceChecker(),
            new RumorServiceChecker(),
            new VendorServiceChecker(),
            new RentRoomServiceChecker(),
            new RemoveRootOptionChecker(), // Needs to be before dialogue quest lock unlock processor
            new EmotionChecker(_emotionClassifierProvider.EmotionClassifier),
        ];

        if (context.Quest.IsDialogueQuest()) {
            ConversationProcessors.Add(new DialogueQuestLockUnlockProcessor(context));
        }
    }

    // Runs only for generic dialogue
    public List<IGenericDialogueProcessor> GenericDialogueProcessors { get; }

    // Runs during document parsing
    public List<IDialogueResponseProcessor> ResponseProcessors { get; }

    // Runs during document parsing
    public List<IDialogueTopicInfoProcessor> TopicInfoProcessors { get; }

    // Runs after document parsing
    public List<IDialogueTopicProcessor> TopicProcessors { get; }

    // Runs after document parsing
    public List<IDialogueTopicListProcessor> TopicListProcessors { get; }

    // Runs at the very end
    public List<IConversationProcessor> ConversationProcessors { get; }

    public DialogueProcessor Clone() => new(_context, _emotionClassifierProvider);

    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        foreach (var processor in GenericDialogueProcessors) {
            processor.Process(genericDialogue, topicInfo);
        }
    }

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
