﻿using System.IO;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Topics;
using DialogueImplementationTool.Parser;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Models;
using DialogueImplementationTool.UI.Services;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI.ViewModels;

public sealed class MainWindowVM : ViewModel {
    private const string ModName = "DialogueOutput";
    private readonly Func<EmotionChecker, DialogueProcessor> _dialogueProcessorFactory;
    private readonly Func<IDialogueContext, DialogueProcessor, IDocumentParser, DialogueVM> _dialogueVMFactory;
    private readonly Dictionary<string, Func<string, DialogueProcessor, IDocumentParser>> _documentIterators;
    private readonly Func<string, PythonEmotionClassifier> _emotionClassifierFactory;
    private readonly SkyrimMod _mod;
    private readonly ISpeakerFavoritesSelection _speakerFavoritesSelection;
    private PythonEmotionClassifier? _emotionClassifier;

    public MainWindowVM(
        OutputPathProvider outputPathProvider,
        ISpeakerFavoritesSelection speakerFavoritesSelection,
        Func<IDialogueContext, DialogueProcessor, IDocumentParser, DialogueVM> dialogueVMFactory,
        Func<EmotionChecker, DialogueProcessor> dialogueProcessorFactory,
        Func<string, PythonEmotionClassifier> emotionClassifierFactory,
        Func<string, DialogueProcessor, OpenDocumentTextParser> openDocumentTextIteratorFactory,
        Func<string, DialogueProcessor, DocXDocumentParser> docXIteratorFactory) {
        OutputPathProvider = outputPathProvider;
        _speakerFavoritesSelection = speakerFavoritesSelection;
        _dialogueVMFactory = dialogueVMFactory;
        _dialogueProcessorFactory = dialogueProcessorFactory;
        _emotionClassifierFactory = emotionClassifierFactory;
        _documentIterators = new Dictionary<string, Func<string, DialogueProcessor, IDocumentParser>> {
            { ".odt", openDocumentTextIteratorFactory },
            { ".docx", docXIteratorFactory },
        };
        Extensions = _documentIterators.Keys.ToList();

        _mod = new SkyrimMod(new ModKey(GetNewModName(), ModType.Plugin), SkyrimRelease.SkyrimSE);
        var environment = GameEnvironmentBuilder<ISkyrimMod, ISkyrimModGetter>.Create(GameRelease.SkyrimSE)
            .WithOutputMod(_mod)
            .Build();
        LinkCache = environment.LinkCache;

        TrySetPythonPath();

        this.WhenAnyValue(x => x.QuestFormKey)
            .Subscribe(_ => ValidQuest = !QuestFormKey.IsNull);
    }

    public OutputPathProvider OutputPathProvider { get; }
    public List<string> Extensions { get; }
    public IEnumerable<Type> QuestTypes { get; } = typeof(IQuestGetter).AsEnumerable();
    public string PythonDllPath { get; set; } = string.Empty;
    public ILinkCache<ISkyrimMod, ISkyrimModGetter> LinkCache { get; }
    [Reactive] public FormKey QuestFormKey { get; set; }
    [Reactive] public bool ValidQuest { get; set; }

    private string GetNewModName() {
        var index = 1;
        var fileInfo = new FileInfo(
            Path.Combine(OutputPathProvider.OutputPath, $"{ModName}{index}.esp"));
        while (fileInfo.Exists) {
            index++;
            fileInfo = new FileInfo(
                Path.Combine(OutputPathProvider.OutputPath, $"{ModName}{index}.esp"));
        }

        return ModName + index;
    }

    private void TrySetPythonPath() {
        var paths = Environment.GetEnvironmentVariable("PATH");
        if (paths is null) return;

        foreach (var path in paths.Split(';')) {
            if (!path.Contains("python", StringComparison.OrdinalIgnoreCase)) continue;
            if (!Directory.Exists(path)) continue;

            var filePath = Directory
                .EnumerateFiles(path, "python3*.dll", SearchOption.TopDirectoryOnly)
                // Don't use python3.dll
                .Where(x => !x.Contains("python3.dll"))
                .FirstOrDefault(File.Exists);
            if (filePath is null) continue;

            RefreshPython(filePath);
            break;
        }
    }

    public void RefreshPython(string dllPath) {
        PythonDllPath = dllPath;
        if (_emotionClassifier?.PythonDllPath == PythonDllPath) return;

        try {
            _emotionClassifier?.Dispose();
            _emotionClassifier = null;
            _emotionClassifier = _emotionClassifierFactory(PythonDllPath);
        } catch (Exception e) {
            Console.WriteLine($"Failed to load emotion classifier: {e.Message}");
        }
    }

    public DialogueVM GetDialogueVM(string filePath) {
        var extension = Path.GetExtension(filePath).ToLower();
        var documentParserFactory = _documentIterators.GetValueOrDefault(extension);
        if (documentParserFactory is null) throw new InvalidOperationException($"No parser exists for {extension}");

        var dialogueProcessor =
            _dialogueProcessorFactory(
                new EmotionChecker((IEmotionClassifier?) _emotionClassifier ?? new NullEmotionClassifier()));
        return _dialogueVMFactory(
            new SkyrimDialogueContext(
                LinkCache,
                _mod,
                LinkCache.ResolveContext<IQuest, IQuestGetter>(QuestFormKey).GetOrAddAsOverride(_mod),
                new UISpeakerSelection(LinkCache, _speakerFavoritesSelection)),
            dialogueProcessor,
            documentParserFactory(filePath, dialogueProcessor));
    }
}