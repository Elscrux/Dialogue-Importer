using System.Reactive;
using System.Windows;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Services;
using DynamicData.Binding;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.ViewModels;

public sealed class MainWindowVM : ViewModel, IDocumentImplementedListener {
    private readonly Func<IQuest, QuestDialogueVM> _questDialogueVMFactory;

    public OutputPathProvider OutputPathProvider { get; }
    public IEnvironmentContext EnvironmentContext { get; }
    public PythonEmotionClassifierProvider? PythonEmotionClassifierProvider { get; private set; }

    public IEnumerable<Type> QuestTypes { get; } = [typeof(IQuestGetter)];

    [Reactive] public string Prefix { get; set; }
    public IObservableCollection<QuestDialogueVM> QuestDialogueVMs { get; } =
        new ObservableCollectionExtended<QuestDialogueVM>();
    public IObservableCollection<string> Warnings { get; } = new ObservableCollectionExtended<string>();
    public ReactiveCommand<Unit, Unit> ParseAll { get; }
    public ReactiveCommand<Unit, Unit> AutoParseAll { get; }
    public ReactiveCommand<Unit, Unit> CopyWarnings { get; }

    public MainWindowVM(
        IPrefixProvider prefixProvider,
        IEnvironmentContext environmentContext,
        PythonEmotionClassifierProvider pythonEmotionClassifierProvider,
        OutputPathProvider outputPathProvider,
        Func<IQuest, QuestDialogueVM> questDialogueVMFactory) {
        Prefix = prefixProvider.Prefix;
        EnvironmentContext = environmentContext;
        PythonEmotionClassifierProvider = pythonEmotionClassifierProvider;
        _questDialogueVMFactory = questDialogueVMFactory;
        OutputPathProvider = outputPathProvider;

        ParseAll = ReactiveCommand.Create(ParseAllImpl);
        AutoParseAll = ReactiveCommand.Create(AutoParseAllImpl);
        CopyWarnings = ReactiveCommand.Create(() => Clipboard.SetText(string.Join("\n", Warnings)));

        this.WhenAnyValue(x => x.Prefix)
            .Subscribe(_ => prefixProvider.Prefix = Prefix)
            .DisposeWith(this);

    }

    private void ParseAllImpl() {
        foreach (var questDialogueVM in QuestDialogueVMs) {
            questDialogueVM.ParseAll();
        }
    }

    private void AutoParseAllImpl() {
        foreach (var questDialogueVM in QuestDialogueVMs) {
            questDialogueVM.AutoParseAll();
        }

        EnvironmentContext.Mod.Save(OutputPathProvider.OutputPath, EnvironmentContext.Environment.LoadOrder);
    }

    public void AddQuest(FormKey questFormKey) {
        if (QuestDialogueVMs.Any(q => q.Quest.FormKey == questFormKey)) return;

        var quest = EnvironmentContext.LinkCache.ResolveContext<IQuest, IQuestGetter>(questFormKey)
            .GetOrAddAsOverride(EnvironmentContext.Mod);

        var questDialogueVMFactory = _questDialogueVMFactory(quest);
        QuestDialogueVMs.Add(questDialogueVMFactory);
    }

    public void RemoveQuest(FormKey questFormKey) {
        QuestDialogueVMs.RemoveWhere(q => q.Quest.FormKey == questFormKey);
    }

    public void OnDocumentImplemented(DocumentVM documentVM, bool wasAutoApplied) {
        Warnings.AddRange(documentVM.Context.Issues);
    }
}
