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
    private readonly IDialogueContext _context;
    private readonly OutputPathProvider _outputPathProvider;
    private readonly PapyrusCompilerWrapper _papyrusCompilerWrapper;
    private readonly Func<IDocumentParser, IReadOnlyList<DialogueSelection>, DialogueVM> _dialogueVMFactory;
    private readonly Action<DocumentVM> _deleteDocument;
    private readonly Func<IDialogueContext, DialogueProcessor> _dialogueProcessorFactory;
    private readonly DialogueSelectionsCache _dialogueSelectionsCache;

    public string FilePath => _documentParser.FilePath;
    [Reactive] public DocumentStatus Status { get; set; } = DocumentStatus.NotLoaded;
    [Reactive] public bool HasCachedSelections { get; set; }
    public List<DialogueSelection> DialogueSelections { get; }
    public ReactiveCommand<Unit, Unit> AutoParse { get; }
    public ReactiveCommand<Unit, Unit> ManualParse { get; }
    public ReactiveCommand<Unit, Unit> Delete { get; }

    public DocumentVM(
        IDocumentParser documentParser,
        IDialogueContext context,
        OutputPathProvider outputPathProvider,
        PapyrusCompilerWrapper papyrusCompilerWrapper,
        Func<IDocumentParser, IReadOnlyList<DialogueSelection>, DialogueVM> dialogueVMFactory,
        Action<DocumentVM> deleteDocument,
        Func<IDialogueContext, DialogueProcessor> dialogueProcessorFactory) {
        _documentParser = documentParser;
        _context = context;
        _outputPathProvider = outputPathProvider;
        _papyrusCompilerWrapper = papyrusCompilerWrapper;
        _dialogueVMFactory = dialogueVMFactory;
        _deleteDocument = deleteDocument;
        _dialogueProcessorFactory = dialogueProcessorFactory;
        _dialogueSelectionsCache = new DialogueSelectionsCache(_documentParser.FilePath);
        DialogueSelections = _dialogueSelectionsCache.LoadSelection();
        HasCachedSelections = DialogueSelections.Count > 0;

        AutoParse = ReactiveCommand.Create(ImplementDialogue);
        ManualParse = ReactiveCommand.Create(LaunchParser);
        Delete = ReactiveCommand.Create(DeleteDocument);
    }

    public void ImplementDialogue() {
        ImplementDialogue(true);
        _context.Mod.Save(_outputPathProvider.OutputPath);
    }

    private void ImplementDialogue(bool autoApply) {
        Status = DocumentStatus.InProgress;
        _context.AutoApplyProvider.AutoApply = autoApply;

        BaseDialogueFactory.ImplementDialogue(
            _context,
            _documentParser,
            _outputPathProvider,
            _papyrusCompilerWrapper,
            _dialogueProcessorFactory(_context),
            DialogueSelections);

        Status = DocumentStatus.Implemented;
    }

    public void LaunchParser() {
        var selections = _dialogueSelectionsCache.LoadSelection();
        var dialogueVM = _dialogueVMFactory(_documentParser, selections);

        var processDialogue = new ProcessDialogue(dialogueVM);
        processDialogue.ShowDialog();

        // Skip if no selections were made
        if (dialogueVM.DialogueSelections.TrueForAll(s => s.SelectedTypes.Count == 0)) return;

        _dialogueSelectionsCache.SaveSelection(dialogueVM.DialogueSelections);
        HasCachedSelections = true;

        DialogueSelections.Clear();
        DialogueSelections.AddRange(dialogueVM.DialogueSelections);

        ImplementDialogue(false);

        _context.Mod.Save(_outputPathProvider.OutputPath);
    }

    public void DeleteDocument() => _deleteDocument(this);
}

public enum DocumentStatus {
    NotLoaded,
    InProgress,
    Implemented,
}
