using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Windows.Input;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.ViewModels;

public sealed class IterableDialogueConfigVM : ViewModel {
    private readonly IDocumentIterator _documentParser;

    public ISpeakerFavoritesSelection SpeakerFavoritesSelection { get; }

    public ILinkCache LinkCache { get; }

    /*====================================================
        Dialogue List
    ====================================================*/
    public List<DialogueSelection> DialogueSelections { get; } = [];

    [Reactive] public string PreviewText { get; set; } = string.Empty;

    [Reactive] public int Index { get; set; }
    public int LastIndex => _documentParser.LastIndex;

    [Reactive] public bool IsNotFirstIndex { get; set; }
    [Reactive] public bool IsNotLastIndex { get; set; }

    [Reactive] public bool GreetingSelected { get; set; }
    [Reactive] public bool FarewellSelected { get; set; }
    [Reactive] public bool IdleSelected { get; set; }
    [Reactive] public bool DialogueSelected { get; set; }
    [Reactive] public bool GenericSceneSelected { get; set; }
    [Reactive] public bool QuestSceneSelected { get; set; }


    /*====================================================
        NPC
    ====================================================*/
    public IEnumerable<Type> SpeakerTypes { get; } = [
        typeof(INpcGetter),
        typeof(IFactionGetter),
        typeof(IVoiceTypeGetter),
        typeof(IFormListGetter),
    ];

    [Reactive] public FormKey SpeakerFormKey { get; set; }
    [Reactive] public bool ValidSpeaker { get; set; }

    public ICommand SetSpeaker { get; }
    public ICommand SelectIndex { get; }
    public ICommand OpenDocument { get; }
    public ICommand ApplyAll { get; }

    public ICommand BacktrackMany { get; }
    public ICommand Previous { get; }
    public ICommand Next { get; }
    public ICommand SkipMany { get; }
    public string Title { get; }
    [Reactive] public bool UseGetIsAliasRef { get; set; }

    public IterableDialogueConfigVM(
        EnvironmentContext context,
        IDocumentIterator documentParser,
        ISpeakerFavoritesSelection speakerFavoritesSelection) {
        SpeakerFavoritesSelection = speakerFavoritesSelection;
        _documentParser = documentParser;
        Index = _documentParser.Index;
        LinkCache = context.LinkCache;
        Title = Path.GetFileName(documentParser.FilePath);

        // Set buttons to unchecked
        GreetingSelected = FarewellSelected =
            IdleSelected = DialogueSelected = GenericSceneSelected = QuestSceneSelected = false;

        // Set up selections
        DialogueSelections.Clear();
        for (var i = 0; i <= _documentParser.LastIndex; i++) DialogueSelections.Add(new DialogueSelection());

        Task.Run(() => {
            foreach (var speakerType in SpeakerTypes) {
                LinkCache.Warmup(speakerType);
            }
        });

        SetSpeaker = ReactiveCommand.Create<ISpeaker>(speaker => SpeakerFormKey = speaker.FormKey);
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

        OpenDocument = ReactiveCommand.Create(() => {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo {
                FileName = $"\"{_documentParser.FilePath}\"",
                UseShellExecute = true,
                Verb = "open",
            };
            process.Start();
        });

        ApplyAll = ReactiveCommand.Create<DialogueType>(type => {
            foreach (var dialogueSelection in DialogueSelections) {
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
                if (DialogueSelections.Count <= Index) return;

                if (DialogueSelections[Index].Speaker == FormKey.Null) {
                    // Keep current speaker for fresh dialogue and set in list
                    DialogueSelections[Index].Speaker = SpeakerFormKey;
                    DialogueSelections[Index].UseGetIsAliasRef = UseGetIsAliasRef;
                } else {
                    // Load speaker from list
                    SpeakerFormKey = DialogueSelections[Index].Speaker;
                    UseGetIsAliasRef = DialogueSelections[Index].UseGetIsAliasRef;
                }

                GreetingSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.Greeting);
                FarewellSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.Farewell);
                IdleSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.Idle);
                DialogueSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.Dialogue);
                GenericSceneSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.GenericScene);
                QuestSceneSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.QuestScene);
            });

        this.WhenAnyValue(v => v.SpeakerFormKey)
            .Subscribe(_ => {
                ValidSpeaker = SpeakerFormKey != FormKey.Null;
                if (DialogueSelections.Count > Index) DialogueSelections[Index].Speaker = SpeakerFormKey;
                speakerFavoritesSelection.AddSpeaker(new NpcSpeaker(LinkCache, SpeakerFormKey));
            });

        this.WhenAnyValue(v => v.UseGetIsAliasRef)
            .Subscribe(x => {
                if (DialogueSelections.Count > Index) DialogueSelections[Index].UseGetIsAliasRef = x;
            });

        SetupSelectionSubscription(vm => vm.GreetingSelected, DialogueType.Greeting);
        SetupSelectionSubscription(vm => vm.FarewellSelected, DialogueType.Farewell);
        SetupSelectionSubscription(vm => vm.IdleSelected, DialogueType.Idle);
        SetupSelectionSubscription(vm => vm.DialogueSelected, DialogueType.Dialogue);
        SetupSelectionSubscription(vm => vm.GenericSceneSelected, DialogueType.GenericScene);
        SetupSelectionSubscription(vm => vm.QuestSceneSelected, DialogueType.QuestScene);

        void SetupSelectionSubscription(Expression<Func<IterableDialogueConfigVM, bool>> property, DialogueType type) {
            this.WhenAnyValue(property)
                .Subscribe(selected => {
                    if (DialogueSelections.Count <= Index) return;

                    if (selected) {
                        DialogueSelections[Index].SelectedTypes.Add(type);
                    } else {
                        DialogueSelections[Index].SelectedTypes.Remove(type);
                    }
                });
        }
    }

    public void SetSelections(IReadOnlyList<DialogueSelection> dialogueSelections) {
        if (dialogueSelections.Count != DialogueSelections.Count) {
            throw new InvalidOperationException("Selection count mismatch");
        }

        for (var i = 0; i < dialogueSelections.Count; i++) {
            DialogueSelections[i] = dialogueSelections[i];
        }
    }

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
