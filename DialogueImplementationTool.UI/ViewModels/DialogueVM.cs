using System.IO;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Windows.Input;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Script;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Services;
using Mutagen.Bethesda.Json;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.ViewModels;

public sealed partial class DialogueVM : ViewModel {
    private readonly IDocumentParser _documentParser;

    public DialogueVM(
        IDialogueContext context,
        DialogueProcessor dialogueProcessor,
        IDocumentParser documentParser,
        ISpeakerFavoritesSelection speakerFavoritesSelection,
        OutputPathProvider outputPathProvider) {
        SpeakerFavoritesSelection = speakerFavoritesSelection;
        _documentParser = documentParser;
        Index = _documentParser.Index;
        LinkCache = context.LinkCache;
        var compiler = new PapyrusCompilerWrapper(context.Environment);

        Title = Path.GetFileName(documentParser.FilePath);
        SavedSession = false;

        //Clear dialogue data
        DialogueTypeList.Clear();
        for (var i = 0; i <= _documentParser.LastIndex; i++) DialogueTypeList.Add(new DialogueSelection());
        LoadSelection();

        //Set buttons to unchecked
        GreetingSelected = FarewellSelected =
            IdleSelected = DialogueSelected = GenericSceneSelected = QuestSceneSelected = false;

        Task.Run(() => {
            foreach (var speakerType in SpeakerTypes) {
                LinkCache.Warmup(speakerType);
            }
        });

        SetSpeaker = ReactiveCommand.Create((FormKey formKey) => SpeakerFormKey = formKey);
        SelectIndex = ReactiveCommand.Create<string>(indexStr => {
            switch (int.Parse(indexStr)) {
                case 1:
                    if (ValidSpeaker) GreetingSelected = !GreetingSelected;
                    break;
                case 2:
                    if (ValidSpeaker) FarewellSelected = !FarewellSelected;
                    break;
                case 3:
                    if (ValidSpeaker) IdleSelected = !IdleSelected;
                    break;
                case 4:
                    if (ValidSpeaker) DialogueSelected = !DialogueSelected;
                    break;
                case 5:
                    GenericSceneSelected = !GenericSceneSelected;
                    break;
                case 6:
                    QuestSceneSelected = !QuestSceneSelected;
                    break;
            }
        });

        Save = ReactiveCommand.Create(() => {
            SaveSelection();

            ImplementDialogue(context, dialogueProcessor);

            var directoryInfo = new DirectoryInfo(Path.Combine(outputPathProvider.OutputPath, context.Mod.ModKey.Name));
            var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, context.Mod.ModKey.FileName));

            if (fileInfo.Directory is { Exists: false }) fileInfo.Directory?.Create();
            context.Mod.WriteToBinaryParallel(fileInfo.FullName);
            foreach (var (fileName, content) in context.Scripts) {
                var scriptsDirectory = Path.Combine(directoryInfo.FullName, "Scripts");
                var scriptsSourceDirectory = Path.Combine(scriptsDirectory, "Source");
                Directory.CreateDirectory(scriptsSourceDirectory);
                var sourcePath = Path.Combine(scriptsSourceDirectory, fileName + ".psc");
                // var compiledPath = Path.Combine(scriptsDirectory, fileName + ".pex");
                File.WriteAllText(sourcePath, content);
                compiler.Compile(sourcePath, scriptsDirectory, scriptsSourceDirectory);
            }

            SavedSession = true;
        });

        ApplyAll = ReactiveCommand.Create<DialogueType>(type => {
            foreach (var dialogueSelection in DialogueTypeList) {
                dialogueSelection.SelectedTypes.Clear();
                dialogueSelection.SelectedTypes.Add(type);
            }

            GreetingSelected = false;
            FarewellSelected = false;
            IdleSelected = false;
            DialogueSelected = false;
            GenericSceneSelected = false;
            QuestSceneSelected = false;

            switch (type) {
                case DialogueType.Greeting:
                    GreetingSelected = true;
                    break;
                case DialogueType.Farewell:
                    FarewellSelected = true;
                    break;
                case DialogueType.Idle:
                    IdleSelected = true;
                    break;
                case DialogueType.Dialogue:
                    DialogueSelected = true;
                    break;
                case DialogueType.GenericScene:
                    GenericSceneSelected = true;
                    break;
                case DialogueType.QuestScene:
                    QuestSceneSelected = true;
                    break;
            }
        });

        BacktrackMany = ReactiveCommand.Create(() => {
            _documentParser.BacktrackMany();
            Index = _documentParser.Index;
            RefreshPreview(false);
        });

        Previous = ReactiveCommand.Create(() => {
            _documentParser.Previous();
            Index = _documentParser.Index;
            RefreshPreview(false);
        });

        Next = ReactiveCommand.Create(() => {
            _documentParser.Next();
            Index = _documentParser.Index;
            RefreshPreview(true);
        });

        SkipMany = ReactiveCommand.Create(() => {
            _documentParser.SkipMany();
            Index = _documentParser.Index;
            RefreshPreview(true);
        });

        this.WhenAnyValue(v => v.Index)
            .Subscribe(_ => {
                IsNotFirstIndex = Index > 0;
                IsNotLastIndex = Index < _documentParser.LastIndex;
                if (DialogueTypeList.Count <= Index) return;

                if (DialogueTypeList[Index].Speaker == FormKey.Null) {
                    // Keep current speaker for fresh dialogue and set in list
                    DialogueTypeList[Index].Speaker = SpeakerFormKey;
                    DialogueTypeList[Index].UseGetIsAliasRef = UseGetIsAliasRef;
                } else {
                    // Load speaker from list
                    SpeakerFormKey = DialogueTypeList[Index].Speaker;
                    UseGetIsAliasRef = DialogueTypeList[Index].UseGetIsAliasRef;
                }

                GreetingSelected = DialogueTypeList[Index].SelectedTypes.Contains(DialogueType.Greeting);
                FarewellSelected = DialogueTypeList[Index].SelectedTypes.Contains(DialogueType.Farewell);
                IdleSelected = DialogueTypeList[Index].SelectedTypes.Contains(DialogueType.Idle);
                DialogueSelected = DialogueTypeList[Index].SelectedTypes.Contains(DialogueType.Dialogue);
                GenericSceneSelected = DialogueTypeList[Index].SelectedTypes.Contains(DialogueType.GenericScene);
                QuestSceneSelected = DialogueTypeList[Index].SelectedTypes.Contains(DialogueType.QuestScene);
            });

        this.WhenAnyValue(v => v.SpeakerFormKey)
            .Subscribe(_ => {
                ValidSpeaker = SpeakerFormKey != FormKey.Null;
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].Speaker = SpeakerFormKey;
                speakerFavoritesSelection.AddSpeaker(new NpcSpeaker(LinkCache, SpeakerFormKey));
            });

        this.WhenAnyValue(v => v.UseGetIsAliasRef)
            .Subscribe(x => {
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].UseGetIsAliasRef = x;
            });

        this.WhenAnyValue(v => v.SkipSceneSpeakerSelection)
            .Subscribe(skipSceneSpeakerSelection => {
                context.SkipSceneSpeakerSelection = skipSceneSpeakerSelection;
            });

        SetupSelectionSubscription(vm => vm.GreetingSelected, DialogueType.Greeting);
        SetupSelectionSubscription(vm => vm.FarewellSelected, DialogueType.Farewell);
        SetupSelectionSubscription(vm => vm.IdleSelected, DialogueType.Idle);
        SetupSelectionSubscription(vm => vm.DialogueSelected, DialogueType.Dialogue);
        SetupSelectionSubscription(vm => vm.GenericSceneSelected, DialogueType.GenericScene);
        SetupSelectionSubscription(vm => vm.QuestSceneSelected, DialogueType.QuestScene);

        void SetupSelectionSubscription(Expression<Func<DialogueVM, bool>> property, DialogueType type) {
            this.WhenAnyValue(property)
                .Subscribe(selected => {
                    if (DialogueTypeList.Count <= Index) return;

                    if (selected) {
                        DialogueTypeList[Index].SelectedTypes.Add(type);
                    } else {
                        DialogueTypeList[Index].SelectedTypes.Remove(type);
                    }
                });
        }
    }

    private void ImplementDialogue(IDialogueContext context, DialogueProcessor dialogueProcessor) {
        var conversation =
            BaseDialogueFactory.PrepareDialogue(context, dialogueProcessor, _documentParser, DialogueTypeList);

        // Conversation wide processing
        dialogueProcessor.Process(conversation);

        // Actually create the dialogues
        foreach (var dialogue in conversation) {
            dialogue.Factory.Create(dialogue);
        }
    }

#region SelectionPersistance
    [GeneratedRegex("[\\/:*?\"<>|]")]
    private static partial Regex IllegalFileNameRegex();

    private string SelectionsPath =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Selections",
            IllegalFileNameRegex().Replace(_documentParser.FilePath + ".selections", string.Empty));

    private readonly JsonSerializerSettings _serializerSettings = new() {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        Converters = {
            JsonConvertersMixIn.FormKey,
            JsonConvertersMixIn.ModKey,
        },
    };

    private void LoadSelection() {
        var selectionsPath = SelectionsPath;
        if (!File.Exists(selectionsPath)) return;

        var text = File.ReadAllText(selectionsPath);
        var selections = JsonConvert.DeserializeObject<List<DialogueSelection>>(text, _serializerSettings);
        if (selections is null) return;

        for (var i = 0; i < selections.Count; i++) {
            if (DialogueTypeList.Count <= i) {
                DialogueTypeList.Add(selections[i]);
            } else {
                DialogueTypeList[i] = selections[i];
            }
        }
    }

    private void SaveSelection() {
        var selections = JsonConvert.SerializeObject(DialogueTypeList, _serializerSettings);
        var directoryName = Path.GetDirectoryName(SelectionsPath);
        if (directoryName is null) return;

        if (!Directory.Exists(directoryName)) {
            Directory.CreateDirectory(directoryName);
        }

        File.WriteAllText(SelectionsPath, selections);
    }
#endregion

    public ISpeakerFavoritesSelection SpeakerFavoritesSelection { get; }

    public ILinkCache LinkCache { get; }

    /*====================================================
        Dialogue List
    ====================================================*/
    public List<DialogueSelection> DialogueTypeList { get; } = [];

    public bool SavedSession { get; private set; }

    [Reactive]
    public string PreviewText { get; set; } = string.Empty;

    [Reactive] public int Index { get; set; }
    public int LastIndex => _documentParser.LastIndex;

    [Reactive]
    public bool IsNotFirstIndex { get; set; }

    [Reactive]
    public bool IsNotLastIndex { get; set; }

    [Reactive]
    public bool GreetingSelected { get; set; }

    [Reactive]
    public bool FarewellSelected { get; set; }

    [Reactive]
    public bool IdleSelected { get; set; }

    [Reactive]
    public bool DialogueSelected { get; set; }

    [Reactive]
    public bool GenericSceneSelected { get; set; }

    [Reactive]
    public bool QuestSceneSelected { get; set; }


    /*====================================================
        NPC
    ====================================================*/
    public IEnumerable<Type> SpeakerTypes { get; } = [
        typeof(INpcGetter),
        typeof(IFactionGetter),
        typeof(IVoiceTypeGetter),
        typeof(IFormListGetter),
    ];

    [Reactive]
    public FormKey SpeakerFormKey { get; set; }

    [Reactive]
    public bool ValidSpeaker { get; set; }

    public ICommand SetSpeaker { get; }
    public ICommand SelectIndex { get; }
    public ICommand Save { get; }
    public ICommand ApplyAll { get; }

    public ICommand BacktrackMany { get; }
    public ICommand Previous { get; }
    public ICommand Next { get; }
    public ICommand SkipMany { get; }
    public string Title { get; }
    [Reactive] public bool UseGetIsAliasRef { get; set; }
    [Reactive] public bool SkipSceneSpeakerSelection { get; set; } = true;

    public void RefreshPreview(bool forward) {
        var preview = string.Empty;
        var tries = 0;
        while (string.IsNullOrWhiteSpace(preview) && tries < 10) {
            preview = _documentParser.PreviewCurrent();
            if (string.IsNullOrEmpty(preview)) {
                if (forward) {
                    _documentParser.Next();
                } else {
                    _documentParser.Previous();
                }
                Index = _documentParser.Index;
            } else {
                PreviewText = preview;
            }

            tries++;
        }
    }
}
