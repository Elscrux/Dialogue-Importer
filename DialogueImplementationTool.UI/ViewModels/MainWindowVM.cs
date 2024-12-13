using System.Collections;
using System.IO;
using System.Reactive;
using System.Windows;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Services;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.ViewModels;

public sealed class MainWindowVM : ViewModel {
    private readonly AutoApplyProvider _autoApplyProvider;
    private readonly Dictionary<string, Func<string, IDocumentParser>> _documentParsers;
    private readonly IFormKeySelection _formKeySelection;
    private readonly ISpeakerFavoritesSelection _speakerFavoritesSelection;
    private readonly Func<IDocumentParser, IDialogueContext, Action<DocumentVM>, Action<DocumentVM, bool>, DocumentVM>
        _documentVMFactory;

    public OutputPathProvider OutputPathProvider { get; }
    public EnvironmentContext EnvironmentContext { get; }
    public PythonEmotionClassifierProvider PythonEmotionClassifierProvider { get; }
    public List<string> Extensions { get; }
    public IEnumerable<Type> QuestTypes { get; } = [typeof(IQuestGetter)];
    [Reactive] public FormKey QuestFormKey { get; set; }
    [Reactive] public bool ValidQuest { get; set; }
    [Reactive] public string Prefix { get; set; } = string.Empty;
    public IObservableCollection<DocumentVM> Documents { get; } = new ObservableCollectionExtended<DocumentVM>();
    public IObservableCollection<string> Warnings { get; } = new ObservableCollectionExtended<string>();
    public ReactiveCommand<IList, Unit> DeleteDocuments { get; }
    public ReactiveCommand<Unit, Unit> ParseAll { get; }
    public ReactiveCommand<Unit, Unit> AutoParseAll { get; }
    public ReactiveCommand<Unit, Unit> CopyWarnings { get; }

    public MainWindowVM(
        AutoApplyProvider autoApplyProvider,
        IFormKeySelection formKeySelection,
        EnvironmentContext environmentContext,
        PythonEmotionClassifierProvider pythonEmotionClassifierProvider,
        ISpeakerFavoritesSelection speakerFavoritesSelection,
        Func<IDocumentParser, IDialogueContext, Action<DocumentVM>, Action<DocumentVM, bool>, DocumentVM> documentVMFactory,
        Func<string, OpenDocumentTextParser> openDocumentTextIteratorFactory,
        Func<string, DocXDocumentParser> docXIteratorFactory,
        Func<string, IDialogueContext, CsvDocumentParser> csvIteratorFactory,
        OutputPathProvider outputPathProvider) {
        _autoApplyProvider = autoApplyProvider;
        EnvironmentContext = environmentContext;
        PythonEmotionClassifierProvider = pythonEmotionClassifierProvider;
        _formKeySelection = formKeySelection;
        _speakerFavoritesSelection = speakerFavoritesSelection;
        _documentVMFactory = documentVMFactory;
        OutputPathProvider = outputPathProvider;
        _documentParsers = new Dictionary<string, Func<string, IDocumentParser>> {
            { ".odt", openDocumentTextIteratorFactory },
            { ".docx", docXIteratorFactory },
            { ".csv", path => csvIteratorFactory(path, GetContext(path)) },
        };
        Extensions = _documentParsers.Keys.ToList();

        DeleteDocuments = ReactiveCommand.Create<IList>(list => Documents.RemoveMany(list.OfType<DocumentVM>()));
        ParseAll = ReactiveCommand.Create(ParseAllImpl);
        AutoParseAll = ReactiveCommand.Create(AutoParseAllImpl);
        CopyWarnings = ReactiveCommand.Create(() => Clipboard.SetText(string.Join("\n", Warnings)));

        this.WhenAnyValue(x => x.QuestFormKey)
            .Subscribe(_ => ValidQuest = !QuestFormKey.IsNull);
    }

    private void ParseAllImpl() {
        foreach (var documentVM in Documents) {
            documentVM.LaunchParserConfig();
        }
    }

    private void AutoParseAllImpl() {
        foreach (var document in Documents) {
            document.ImplementDialogue();
        }

        EnvironmentContext.Mod.Save(OutputPathProvider.OutputPath, EnvironmentContext.Environment.LoadOrder);
    }

    private IDocumentParser GetDocumentParser(string filePath) {
        var extension = Path.GetExtension(filePath).ToLower();
        var documentParserFactory = _documentParsers.GetValueOrDefault(extension);
        if (documentParserFactory is null) throw new InvalidOperationException($"No parser exists for {extension}");

        var documentParser = documentParserFactory(filePath);
        return documentParser;
    }

    private SkyrimDialogueContext GetContext(string filePath) {
        return new SkyrimDialogueContext(
            Prefix,
            EnvironmentContext.Environment,
            EnvironmentContext.Mod,
            EnvironmentContext.LinkCache.ResolveContext<IQuest, IQuestGetter>(QuestFormKey)
                .GetOrAddAsOverride(EnvironmentContext.Mod),
            new UISpeakerSelection(EnvironmentContext.LinkCache, _speakerFavoritesSelection, filePath),
            _autoApplyProvider,
            _speakerFavoritesSelection,
            _formKeySelection);
    }

    public void AddDocument(string documentFilePath) {
        var documentVM = _documentVMFactory(
            GetDocumentParser(documentFilePath),
            GetContext(documentFilePath),
            doc => Documents.Remove(doc),
            DocumentImplemented);

        Documents.Add(documentVM);
    }

    private void DocumentImplemented(DocumentVM document, bool wasAutoApplied) {
        Warnings.AddRange(document.Context.Issues);
    }
}
