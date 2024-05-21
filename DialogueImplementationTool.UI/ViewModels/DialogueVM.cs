using System.IO;
using System.Linq.Expressions;
using System.Windows.Input;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.UI.Models;
using DialogueImplementationTool.UI.Services;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.ViewModels;

public sealed class DialogueVM : ViewModel {
    private readonly IDocumentParser _documentParser;

    public DialogueVM(
        IDialogueContext context,
        DialogueProcessor dialogueProcessor,
        IDocumentParser documentParser,
        ISpeakerFavoritesSelection speakerFavoritesSelection,
        OutputPathProvider outputPathProvider) {
        SpeakerFavoritesSelection = speakerFavoritesSelection;
        _documentParser = documentParser;
        LinkCache = context.LinkCache;

        Title = Path.GetFileName(documentParser.FilePath);
        SavedSession = false;

        //Clear dialogue data
        DialogueTypeList.Clear();
        for (var i = 0; i <= _documentParser.LastIndex; i++) DialogueTypeList.Add(new DialogueSelection());

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
            ImplementDialogue(context, dialogueProcessor);

            var fileInfo = new FileInfo(Path.Combine(outputPathProvider.OutputPath, context.Mod.ModKey.FileName));

            if (fileInfo.Directory is { Exists: false }) fileInfo.Directory?.Create();
            context.Mod.WriteToBinaryParallel(fileInfo.FullName);

            SavedSession = true;
        });

        BacktrackMany = ReactiveCommand.Create(() => {
            _documentParser.BacktrackMany();
            RefreshPreview(false);
        });

        Previous = ReactiveCommand.Create(() => {
            _documentParser.Previous();
            RefreshPreview(false);
        });

        Next = ReactiveCommand.Create(() => {
            _documentParser.Next();
            RefreshPreview(true);
        });

        SkipMany = ReactiveCommand.Create(() => {
            _documentParser.SkipMany();
            RefreshPreview(true);
        });

        this.WhenAnyValue(v => v._documentParser.Index)
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

    public ISpeakerFavoritesSelection SpeakerFavoritesSelection { get; }

    public ILinkCache LinkCache { get; }

    /*====================================================
        Dialogue List
    ====================================================*/
    public List<DialogueSelection> DialogueTypeList { get; } = [];

    public bool SavedSession { get; private set; }

    [Reactive]
    public string PreviewText { get; set; } = string.Empty;

    public int Index => _documentParser.Index;

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

    public ICommand BacktrackMany { get; }
    public ICommand Previous { get; }
    public ICommand Next { get; }
    public ICommand SkipMany { get; }
    public string Title { get; }
    [Reactive] public bool UseGetIsAliasRef { get; set; }

    public void RefreshPreview(bool forward) {
        var preview = string.Empty;
        var tries = 0;
        while (string.IsNullOrWhiteSpace(preview) && tries < 10) {
            preview = _documentParser.PreviewCurrent();
            if (string.IsNullOrEmpty(preview)) {
                if (forward)
                    _documentParser.Next();
                else
                    _documentParser.Previous();
            } else {
                PreviewText = preview;
            }

            tries++;
        }
    }
}
