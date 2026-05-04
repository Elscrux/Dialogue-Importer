using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Models;
using DialogueImplementationTool.UI.Services;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.ViewModels;

using Conversation = List<GeneratedDialogue>;

public sealed partial class IterableDialogueConfigVM : ViewModel {
    private readonly IDocumentIterator _documentParser;
    private readonly ISpeakerSelectionRepository _speakerSelectionRepository;

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
        typeof(ITalkingActivatorGetter),
    ];

    [Reactive] public FormKey SpeakerFormKey { get; set; }
    [Reactive] public IFormLinkGetter SpeakerLink { get; set; } = new FormLinkInformation(FormKey.Null, typeof(INpcGetter));
    [Reactive] public bool ValidSpeaker { get; set; }

    /*====================================================
        Scene Actors
    ====================================================*/
    [Reactive] public bool IsSceneSelected { get; set; }
    public ObservableCollection<AliasSpeakerSelection> CurrentSceneSpeakers { get; } = [];

    // Store scene speakers for each dialogue index
    private readonly Dictionary<int, List<AliasSpeakerSelection>> _sceneSpeakersPerIndex = new();

    public ICommand SetSpeaker { get; }
    public ICommand SelectIndex { get; }
    public ICommand OpenDocument { get; }
    public ICommand ApplyAll { get; }

    public ICommand ToStart { get; }
    public ICommand Previous { get; }
    public ICommand Next { get; }
    public ICommand ToEnd { get; }
    public string Title { get; }
    [Reactive] public bool UseGetIsAliasRef { get; set; }
    [Reactive] public bool HasNpcSelected { get; set; }

    public IterableDialogueConfigVM(
        IEnvironmentContext context,
        IDocumentIterator documentParser,
        AutomaticSpeakerSelection automaticSpeakerSelection,
        ISpeakerSelectionRepository speakerSelectionRepository,
        ISpeakerFavoritesSelection speakerFavoritesSelection) {
        SpeakerFavoritesSelection = speakerFavoritesSelection;
        _documentParser = documentParser;
        _speakerSelectionRepository = speakerSelectionRepository;
        Index = _documentParser.Index;
        LinkCache = context.LinkCache;
        Title = Path.GetFileNameWithoutExtension(documentParser.FilePath);

        // Set buttons to unchecked
        GreetingSelected = FarewellSelected =
            IdleSelected = DialogueSelected = GenericSceneSelected = QuestSceneSelected = false;

        // Set up selections
        DialogueSelections.Clear();
        for (var i = 0; i <= _documentParser.LastIndex; i++) DialogueSelections.Add(new DialogueSelection());

        // Try to parse initial speaker from document name
        TrySetSpeaker(automaticSpeakerSelection);

        Task.Run(() => {
            foreach (var speakerType in SpeakerTypes) {
                LinkCache.Warmup(speakerType);
            }
        });

        SetSpeaker = ReactiveCommand.Create<ISpeaker>(speaker => SpeakerFormKey = speaker.FormLink.FormKey);
        SelectIndex = ReactiveCommand.Create<string>(indexStr => {
            switch (int.Parse(indexStr)) {
                case 1:
                    GreetingSelected = !GreetingSelected;
                    break;
                case 2:
                    FarewellSelected = !FarewellSelected;
                    break;
                case 3:
                    IdleSelected = !IdleSelected;
                    break;
                case 4:
                    DialogueSelected = !DialogueSelected;
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

        ToStart = ReactiveCommand.Create(() => {
            SaveCurrentSceneSpeakers();
            while (_documentParser.Index > 0) {
                _documentParser.Previous();
            }
            Index = _documentParser.Index;
            RefreshPreview(false);
        });

        Previous = ReactiveCommand.Create(() => {
            SaveCurrentSceneSpeakers();
            _documentParser.Previous();
            Index = _documentParser.Index;
            RefreshPreview(false);
        });

        Next = ReactiveCommand.Create(() => {
            SaveCurrentSceneSpeakers();
            _documentParser.Next();
            Index = _documentParser.Index;
            RefreshPreview(true);
        });

        ToEnd = ReactiveCommand.Create(() => {
            SaveCurrentSceneSpeakers();
            while (_documentParser.Index < _documentParser.LastIndex) {
                _documentParser.Next();
            }
            Index = _documentParser.Index;
            RefreshPreview(true);
        });

        this.WhenAnyValue(v => v.Index)
            .Subscribe(_ => RefreshPreview());

        this.WhenAnyValue(v => v.SpeakerFormKey)
            .Subscribe(speaker => {
                if (speaker.IsNull) {
                    SpeakerLink = new FormLinkInformation(FormKey.Null, typeof(INpcGetter));
                    HasNpcSelected = false;
                } else {
                    var speakerType = GetSpeakerType(speaker);
                    SpeakerLink = new FormLinkInformation(speaker, speakerType);
                    HasNpcSelected = speakerType.InheritsFrom(typeof(INpcGetter));
                }
            });

        this.WhenAnyValue(x => x.HasNpcSelected)
            .Subscribe(npcSelected => {
                if (npcSelected) return;

                UseGetIsAliasRef = false;
            });

        this.WhenAnyValue(v => v.SpeakerLink)
            .Subscribe(speaker => {
                if (DialogueSelections.Count > Index) DialogueSelections[Index].Speaker = speaker;
                speakerFavoritesSelection.AddSpeaker(new NpcSpeaker(LinkCache, speaker));
            });

        this.WhenAnyValue(v => v.UseGetIsAliasRef)
            .Subscribe(x => {
                if (DialogueSelections.Count > Index) DialogueSelections[Index].UseGetIsAliasRef = x;
            });

        // Watch for scene selection changes
        this.WhenAnyValue(
                x => x.GenericSceneSelected,
                x => x.QuestSceneSelected,
                (generic, quest) => generic || quest)
            .Subscribe(isScene => {
                IsSceneSelected = isScene;
                if (isScene) {
                    DetectAndLoadSceneSpeakers();
                } else {
                    SaveCurrentSceneSpeakers();
                }
            });

        // Watch for changes to scene speaker FormKeys and save immediately
        this.WhenAnyValue(x => x.CurrentSceneSpeakers)
            .Subscribe(speakers => {
                // Subscribe to property changes on each speaker
                foreach (var speaker in speakers) {
                    speaker.WhenAnyValue(s => s.FormKey, s => s.EditorID)
                        .Throttle(TimeSpan.FromMilliseconds(500))
                        .Subscribe(_ => {
                            Debug.WriteLine($"[IterableDialogueConfigVM] Speaker {speaker.Name} changed, auto-saving");
                            _speakerSelectionRepository.SaveSceneSpeakers(CurrentSceneSpeakers);
                        });
                }
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

    public void SaveCurrentSceneSpeakers() {
        if (IsSceneSelected && CurrentSceneSpeakers.Count > 0) {
            // Validate that all speakers are assigned
            if (CurrentSceneSpeakers.Any(s => s.FormKey.IsNull)) {
                Debug.WriteLine("[IterableDialogueConfigVM.SaveCurrentSceneSpeakers] WARNING: Some speakers are not assigned!");
            }

            _sceneSpeakersPerIndex[Index] = CurrentSceneSpeakers.ToList();

            // Also save to the global scene selections file
            _speakerSelectionRepository.SaveSceneSpeakers(CurrentSceneSpeakers);
        }
    }

    public void SaveAllSceneSpeakers() {
        // Save the current one first
        SaveCurrentSceneSpeakers();

        // Now save all stored scene speakers to file
        foreach (var speakers in _sceneSpeakersPerIndex.Values) {
            if (speakers.Count == 0) continue;

            _speakerSelectionRepository.SaveSceneSpeakers(speakers);
        }
    }

    [GeneratedRegex(@"^([^:]+):\s*(.+)")]
    private static partial Regex SceneLineRegex { get; }

    private void DetectAndLoadSceneSpeakers() {
        // First, check if we already have speakers saved for this index
        if (_sceneSpeakersPerIndex.TryGetValue(Index, out var savedSpeakers)) {
            CurrentSceneSpeakers.Clear();
            foreach (var speaker in savedSpeakers) {
                CurrentSceneSpeakers.Add(speaker);
            }
            return;
        }

        if (_documentParser is not ISceneParser sceneParser) {
            return;
        }

        try {
            var minimalProcessor = new MinimalSceneProcessor();
            var topics = sceneParser.ParseScene(minimalProcessor, Index);

            // Extract unique speaker names from the scene
            var speakerNames = new HashSet<string>();
            foreach (var topic in topics) {
                foreach (var topicInfo in topic.TopicInfos) {
                    foreach (var response in topicInfo.Responses) {
                        // Check for SPEAKER= notes
                        foreach (var note in response.StartNotes) {
                            var speaker = SceneResponseProcessor.GetSpeaker(note);
                            if (speaker is not null) {
                                speakerNames.Add(ISpeaker.GetSpeakerName(speaker));
                            }
                        }

                        // Also check for "Name: dialogue" format in response text
                        var match = SceneLineRegex.Match(response.Response);
                        if (match.Success) {
                            var speakerName = ISpeaker.GetSpeakerName(match.Groups[1].Value);
                            speakerNames.Add(speakerName);
                        }
                    }
                }
            }

            CurrentSceneSpeakers.Clear();

            // Try to load from saved file first
            if (_speakerSelectionRepository.TryLoadSceneSpeakers(speakerNames.ToList(), out var speakers)) {
                CurrentSceneSpeakers.AddRange(speakers);
            } else {
                // Create new speaker selections
                foreach (var speakerName in speakerNames.OrderBy(x => x)) {
                    var aliasSpeaker = new AliasSpeakerSelection(LinkCache, SpeakerFavoritesSelection, speakerName);

                    // Try to auto-assign from favorites
                    var matchingSpeaker = SpeakerFavoritesSelection.GetClosestSpeakers(speakerName).FirstOrDefault();
                    if (matchingSpeaker is not null) {
                        aliasSpeaker.FormKey = matchingSpeaker.FormLink.FormKey;
                        aliasSpeaker.EditorID = matchingSpeaker.EditorID;
                    }

                    CurrentSceneSpeakers.Add(aliasSpeaker);
                }
            }

        } catch (Exception ex) {
            Debug.WriteLine($"ERROR: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            CurrentSceneSpeakers.Clear();
        }
    }

    // Minimal processor that only includes the SceneResponseProcessor
    private sealed class MinimalSceneProcessor : IDialogueProcessor {
        private readonly SceneResponseProcessor _sceneResponseProcessor = new();
        private readonly ResponseNoteExtractor _responseNoteExtractor = new();

        public DialogueResponse BuildResponse(IList<FormattedText> textSnippets) {
            var response = new DialogueResponse {
                Response = string.Join(string.Empty, textSnippets.Select(x => x.Text)),
            };
            _sceneResponseProcessor.Process(response, textSnippets);
            _responseNoteExtractor.Process(response, textSnippets);
            return response;
        }

        public void Process(DialogueResponse response, IList<FormattedText> textSnippets) {
            _sceneResponseProcessor.Process(response, textSnippets);
            _responseNoteExtractor.Process(response, textSnippets);
        }

        public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {}
        public void Process(DialogueTopicInfo topicInfo) {}
        public void Process(DialogueTopic topic) {}
        public void Process(List<DialogueTopic> topics) {}
        public void Process(Conversation conversation) {}
    }

    private void RefreshPreview() {
        IsNotFirstIndex = Index > 0;
        IsNotLastIndex = Index < _documentParser.LastIndex;
        if (DialogueSelections.Count <= Index) return;

        if (DialogueSelections[Index].Speaker.IsNull) {
            // Keep current speaker for fresh dialogue and set in list
            DialogueSelections[Index].Speaker = SpeakerLink;
            DialogueSelections[Index].UseGetIsAliasRef = UseGetIsAliasRef;
        } else {
            // Load speaker from list
            SpeakerFormKey = DialogueSelections[Index].Speaker.FormKey;
            UseGetIsAliasRef = DialogueSelections[Index].UseGetIsAliasRef;
        }

        GreetingSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.Greeting);
        FarewellSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.Farewell);
        IdleSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.Idle);
        DialogueSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.Dialogue);
        GenericSceneSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.GenericScene);
        QuestSceneSelected = DialogueSelections[Index].SelectedTypes.Contains(DialogueType.QuestScene);

        // Refresh scene speakers if a scene type is selected
        if (IsSceneSelected) {
            DetectAndLoadSceneSpeakers();
        }
    }

    [GeneratedRegex(@"\[[^\]]*\]|_s|'s|\([^\)]*\)|'s|standard dialogue|dialogue|dialog| - |_|\d+|\.|,",
        RegexOptions.IgnoreCase)]
    private static partial Regex UnnecessaryDocumentNameParts { get; }

    private void TrySetSpeaker(AutomaticSpeakerSelection automaticSpeakerSelection) {
        var documentName = Path.GetFileNameWithoutExtension(_documentParser.FilePath);

        // Cleanup document name
        documentName = UnnecessaryDocumentNameParts.Replace(documentName, string.Empty).Trim();

        var speakers = automaticSpeakerSelection.GetSpeakers<ISpeaker>([documentName], false);
        if (speakers.Count == 1) {
            SpeakerFormKey = speakers[0].FormLink.FormKey;
        }
    }

    public Type GetSpeakerType(FormKey speaker) {
        foreach (var speakerType in SpeakerTypes) {
            if (LinkCache.TryResolve(speaker, speakerType, out var link)) {
                return link.Type;
            }
        }

        throw new InvalidOperationException("Speaker type not found for speaker form key " + speaker);
    }

    public void SetSelections(IReadOnlyList<DialogueSelection> dialogueSelections) {
        if (dialogueSelections.Count == 0) return;

        if (dialogueSelections.Count != DialogueSelections.Count) {
            throw new InvalidOperationException("Selection count mismatch");
        }

        for (var i = 0; i < dialogueSelections.Count; i++) {
            DialogueSelections[i] = dialogueSelections[i];
        }

        RefreshPreview();
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
