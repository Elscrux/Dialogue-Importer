using System.Diagnostics;
using System.Reactive;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Script;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Services;
using DialogueImplementationTool.UI.Views;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.ViewModels;

public sealed class DocumentVM : ViewModel {
    private readonly IDocumentParser _documentParser;
    private readonly OutputPathProvider _outputPathProvider;
    private readonly ScriptWriter _scriptWriter;
    private readonly Func<IDocumentIterator, IterableDialogueConfigVM> _iterableDialogueConfigVMFactory;
    private readonly Action<DocumentVM> _deleteDocument;
    private readonly IDocumentImplementedListener[] _onImplementationComplete;
    private readonly Func<IDialogueContext, DialogueProcessor> _dialogueProcessorFactory;
    private readonly DialogueSelectionsCache _dialogueSelectionsCache;

    public IDialogueContext Context { get; }
    public string FilePath => _documentParser.FilePath;
    [Reactive] public DocumentStatus Status { get; set; } = DocumentStatus.NotLoaded;
    [Reactive] public bool HasCachedSelections { get; set; }
    public List<DialogueSelection> DialogueSelections { get; }
    public ReactiveCommand<Unit, Unit> AutoParse { get; }
    public ReactiveCommand<Unit, Unit> ManualParse { get; }
    public ReactiveCommand<Unit, Unit> Delete { get; }
    public ReactiveCommand<Unit, Unit> Open { get; }

    public DocumentVM(
        IDocumentParser documentParser,
        IDialogueContext context,
        OutputPathProvider outputPathProvider,
        ScriptWriter scriptWriter,
        Func<IDocumentIterator, IterableDialogueConfigVM> iterableDialogueConfigVMFactory,
        Action<DocumentVM> deleteDocument,
        IDocumentImplementedListener[] onImplementationComplete,
        Func<IDialogueContext, DialogueProcessor> dialogueProcessorFactory) {
        _documentParser = documentParser;
        Context = context;
        _outputPathProvider = outputPathProvider;
        _scriptWriter = scriptWriter;
        _iterableDialogueConfigVMFactory = iterableDialogueConfigVMFactory;
        _deleteDocument = deleteDocument;
        _onImplementationComplete = onImplementationComplete;
        _dialogueProcessorFactory = dialogueProcessorFactory;
        _dialogueSelectionsCache = new DialogueSelectionsCache(_documentParser.FilePath);
        DialogueSelections = _dialogueSelectionsCache.LoadSelection();
        HasCachedSelections = DialogueSelections.Count > 0;

        AutoParse = ReactiveCommand.Create(ImplementDialogue);
        ManualParse = ReactiveCommand.Create(LaunchParserConfig);
        Delete = ReactiveCommand.Create(DeleteDocument);
        Open = ReactiveCommand.Create(OpenDocument);
    }

    public void ImplementDialogue() {
        if (Status != DocumentStatus.NotLoaded) return;
        if (HasCachedSelections == false) return;

        ImplementDialogue(true);
        Context.Mod.Save(_outputPathProvider.OutputPath, Context.Environment);
    }

    private void ImplementDialogue(bool autoApply) {
        Status = DocumentStatus.InProgress;
        Context.AutoApplyProvider.AutoApply = autoApply;

        var dialogueProcessor = _dialogueProcessorFactory(Context);
        switch (_documentParser) {
            case IDocumentIterator documentIterator: {
                var conversation = documentIterator.ParseDialogue(Context, dialogueProcessor, DialogueSelections);

                // Actually create the dialogues
                conversation.Create();

                break;
            }
            case IGenericDialogueParser genericDialogueParser:
                var genericDialogue = genericDialogueParser.ParseGenericDialogue(dialogueProcessor);
                var genericDialogueFactory = new GenericDialogueFactory(Context);
                genericDialogueFactory.GenerateDialogue(genericDialogue);
                break;
        }

        // Write scripts
        foreach (var (scriptName, content) in Context.Scripts) {
            _scriptWriter.WriteScript(scriptName, content, Context.Mod.ModKey);
        }

        Status = DocumentStatus.Implemented;

        foreach (var documentImplementedListener in _onImplementationComplete) {
            documentImplementedListener.OnDocumentImplemented(this, autoApply);
        }
    }

    public void LaunchParserConfig() {
        if (Status != DocumentStatus.NotLoaded) return;

        switch (_documentParser) {
            case IDocumentIterator documentIterator: {
                var selections = _dialogueSelectionsCache.LoadSelection();

                var dialogueIteratorVM = _iterableDialogueConfigVMFactory(documentIterator);
                dialogueIteratorVM.SetSelections(selections);

                var processDialogue = new ProcessDialogue(dialogueIteratorVM);
                processDialogue.ShowDialog();

                // Skip if no selections were made
                if (dialogueIteratorVM.DialogueSelections.TrueForAll(s => s.SelectedTypes.Count == 0)) return;

                // Update selections
                _dialogueSelectionsCache.SaveSelection(dialogueIteratorVM.DialogueSelections);
                HasCachedSelections = true;

                DialogueSelections.Clear();
                DialogueSelections.AddRange(dialogueIteratorVM.DialogueSelections);
                break;
            }
            case IDocumentParser: {
                break;
            }
            default: throw new ArgumentOutOfRangeException(nameof(_documentParser));
        }

        ImplementDialogue(false);

        Context.Mod.Save(_outputPathProvider.OutputPath, Context.Environment);
    }

    public void DeleteDocument() => _deleteDocument(this);
    private void OpenDocument() {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo {
            FileName = $"\"{_documentParser.FilePath}\"",
            UseShellExecute = true,
            Verb = "open",
        };
        process.Start();
    }
}

public enum DocumentStatus {
    NotLoaded,
    InProgress,
    Implemented,
}
