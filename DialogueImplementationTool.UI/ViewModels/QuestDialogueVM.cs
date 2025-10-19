using System.Collections;
using System.IO;
using System.Reactive;
using Autofac;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda.Skyrim;
using Noggog.WPF;
using ReactiveUI;
namespace DialogueImplementationTool.UI.ViewModels;

public sealed class QuestDialogueVM : ViewModel {
    private readonly ILifetimeScope _lifetimeScope;
    private readonly Dictionary<string, Func<string, IDocumentParser>> _documentParsers;
    private readonly Func<IDocumentParser, IDialogueContext, Action<DocumentVM>, DocumentVM> _documentVMFactory;

    public IQuest Quest { get; }
    public List<string> Extensions { get; }
    public IEnvironmentContext EnvironmentContext { get; }

    public IObservableCollection<DocumentVM> Documents { get; } = new ObservableCollectionExtended<DocumentVM>();
    public ReactiveCommand<IList, Unit> DeleteDocuments { get; }
    public ReactiveCommand<Unit, Unit> ParseAllCommand { get; }
    public ReactiveCommand<Unit, Unit> AutoParseAllCommand { get; }

    public QuestDialogueVM(
        IQuest quest,
        ILifetimeScope lifetimeScope,
        IEnvironmentContext environmentContext,
        Func<IDocumentParser, IDialogueContext, Action<DocumentVM>, DocumentVM> documentVMFactory,
        Func<string, DocXDocumentParser> docXIteratorFactory,
        Func<string, IDialogueContext, CsvDocumentParser> csvIteratorFactory) {
        _lifetimeScope = lifetimeScope;
        Quest = quest;
        EnvironmentContext = environmentContext;
        _documentVMFactory = documentVMFactory;
        _documentParsers = new Dictionary<string, Func<string, IDocumentParser>> {
            { ".docx", docXIteratorFactory },
            { ".csv", path => csvIteratorFactory(path, GetContext(path)) },
        };
        Extensions = _documentParsers.Keys.ToList();

        DeleteDocuments = ReactiveCommand.Create<IList>(list => Documents.RemoveMany(list.OfType<DocumentVM>()));
        ParseAllCommand = ReactiveCommand.Create(ParseAll);
        AutoParseAllCommand = ReactiveCommand.Create(AutoParseAll);
    }

    public void ParseAll() {
        foreach (var documentVM in Documents) {
            documentVM.LaunchParserConfig();
        }
    }

    public void AutoParseAll() {
        foreach (var document in Documents) {
            document.ImplementDialogue();
        }
    }

    private IDocumentParser GetDocumentParser(string filePath) {
        var extension = Path.GetExtension(filePath).ToLower();
        var documentParserFactory = _documentParsers.GetValueOrDefault(extension);
        if (documentParserFactory is null) throw new InvalidOperationException($"No parser exists for {extension}");

        var documentParser = documentParserFactory(filePath);
        return documentParser;
    }

    private SkyrimDialogueContext GetContext(string filePath) {
        var nestedScope = GetContainer(filePath);

        return nestedScope.Resolve<SkyrimDialogueContext>();
    }

    private ILifetimeScope GetContainer(string filePath) {
        return _lifetimeScope.BeginLifetimeScope(b => {
            b.RegisterInstance(new InjectedDocumentProvider(filePath))
                .As<IDocumentProvider>();

            b.RegisterInstance(Quest)
                .As<IQuest>();
        });
    }

    public void AddDocument(string documentFilePath) {
        var documentVM = _documentVMFactory(
            GetDocumentParser(documentFilePath),
            GetContext(documentFilePath),
            doc => Documents.Remove(doc));

        Documents.Add(documentVM);
    }
}
