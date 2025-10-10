using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class DialogueProcessor : IDialogueProcessor {
    private readonly IDialogueContext _context;
    private readonly IEmotionClassifier _emotionClassifier;

    public DialogueProcessor(IDialogueContext context, IEmotionClassifier emotionClassifier) {
        _context = context;
        _emotionClassifier = emotionClassifier;

        var emotionChecker = new EmotionChecker(_emotionClassifier);

        GenericDialogueProcessors = [
            emotionChecker,
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
            new TrespassingProcessor(_context),
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
            new TopicInfoNoteExtractor(),
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
            new AutoPassChecker(new SkillCheckUtils(_context)),
            new AutoFailChecker(),
        ];

        TopicProcessors = [
            new RandomChecker(), // Should be before the others so it can separate random responses before
            new PlayerIsSexChecker(),
            new PlayerIsRaceChecker(),
            new PlayerIsVampireChecker(),
            new PlayerIsWerewolfChecker(),
            new SuccessFailureSeparator(new SkillCheckUtils(_context)),
            new DeadAliveChecker(_context),
            new BelongsToPreviousChecker(),
            new RemoveEmptyTopicInfos(),
        ];

        TopicListProcessors = [];

        ConversationProcessors = [
            new RemoveRootOptionChecker(), // Needs to be before dialogue quest lock unlock processor
            new CollapseNoteOnlyResponse(), // Needs to be before KeywordLinker
            new KeywordLinker(),
            new BackToOptionsLinker(),
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
            emotionChecker,
        ];

        if (context.Quest.IsDialogueQuest()) {
            var removeRootOptionChecker = ConversationProcessors.FindIndex(p => p is RemoveRootOptionChecker);
            ConversationProcessors.Insert(removeRootOptionChecker + 1, new DialogueQuestLockUnlockProcessor(context));
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

    public DialogueProcessor Clone() => new(_context, _emotionClassifier);

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
