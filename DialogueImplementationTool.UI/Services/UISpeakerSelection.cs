using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Models;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda.Json;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Newtonsoft.Json;
namespace DialogueImplementationTool.UI.Services;

sealed record AliasSelectionDto(IFormLinkGetter FormLink, string? EditorID);

public sealed partial class UISpeakerSelection(
    ILinkCache linkCache,
    AutomaticSpeakerSelection automaticSpeakerSelection,
    ISpeakerFavoritesSelection speakerFavoritesSelection,
    IDocumentProvider documentProvider)
    : ISpeakerSelection {

    private Dictionary<string, Dictionary<string, AliasSelectionDto>>? _savedSceneSelections;

    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames) where T : class, ISpeaker {
        var speakerSelections = new ObservableCollection<AliasSpeakerSelection>(speakerNames
            .Select(s => new AliasSpeakerSelection(linkCache, speakerFavoritesSelection, s))
            .OrderBy(s => s.Name)
            .ToList());

        // Try load from file
        if (LoadSpeakers()) {
            return speakerSelections
                .Select(x => ISpeakerSelection.CreateSpeaker<T>(linkCache, x.FormLink, x.Name, editorId: x.EditorID))
                .ToList();
        }

        // In case all speakers can be matched exactly, apply them directly
        var automaticSpeakersExactMatch = automaticSpeakerSelection.GetSpeakers<T>(speakerNames);
        if (automaticSpeakersExactMatch.Count == speakerNames.Count) {
            SaveSpeakers(automaticSpeakersExactMatch);

            return automaticSpeakersExactMatch;
        }

        // Otherwise, try at least setting recognized speakers automatically
        var automaticSpeakerSuggestions = automaticSpeakerSelection.GetSpeakers<T>(speakerNames, false);
        foreach (var automaticSpeaker in automaticSpeakerSuggestions) {
            var speaker = speakerSelections.FirstOrDefault(s => s.Name == automaticSpeaker.NameNoSpaces || s.Name == automaticSpeaker.Name);
            if (speaker is null) continue;

            speaker.FormKey = automaticSpeaker.FormLink.FormKey;
            speaker.EditorID = automaticSpeaker.EditorID;
        }

        new SceneSpeakerWindow(linkCache, speakerFavoritesSelection, speakerSelections).ShowDialog();

        while (speakerSelections.Any(s => s.FormLink.IsNull)) {
            MessageBox.Show("You must assign every speaker of the scene to an npc");
            new SceneSpeakerWindow(linkCache, speakerFavoritesSelection, speakerSelections).ShowDialog();
        }

        var speakers = speakerSelections
            .Select(x => ISpeakerSelection.CreateSpeaker<T>(linkCache, x.FormLink, x.Name, editorId: x.EditorID))
            .ToList();

        SaveSpeakers(speakers);

        return speakers;

        bool LoadSpeakers() {
            _savedSceneSelections ??= LoadFromFile();
            if (_savedSceneSelections is null) return false;

            if (!_savedSceneSelections.TryGetValue(string.Join('|', speakerNames), out var savedSelections)) return false;

            foreach (var selection in speakerSelections) {
                if (!savedSelections.TryGetValue(selection.Name, out var aliasSelectionDto)) return false;

                selection.FormKey = aliasSelectionDto.FormLink.FormKey;
                selection.EditorID = aliasSelectionDto.EditorID;
            }

            return true;
        }

        Dictionary<string, Dictionary<string, AliasSelectionDto>>? LoadFromFile() {
            if (!File.Exists(SelectionsPath)) return null;

            var text = File.ReadAllText(SelectionsPath);
            var sceneSelections =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, AliasSelectionDto>>>(text,
                    _serializerSettings);

            return sceneSelections;
        }

        void SaveSpeakers(IReadOnlyList<ISpeaker> aliasSpeakers) {
            var selections = new Dictionary<string, AliasSelectionDto>();
            foreach (var speaker in aliasSpeakers) {
                selections[speaker.Name] = new AliasSelectionDto(speaker.FormLink, speaker.EditorID);
                speakerFavoritesSelection.AddSpeaker(speaker);
            }

            _savedSceneSelections ??= LoadFromFile();
            _savedSceneSelections ??= new Dictionary<string, Dictionary<string, AliasSelectionDto>>();
            _savedSceneSelections.Add(string.Join('|', speakerNames), selections);

            var text = JsonConvert.SerializeObject(_savedSceneSelections, _serializerSettings);
            var directoryName = Path.GetDirectoryName(SelectionsPath);
            if (directoryName is null) return;

            if (!Directory.Exists(directoryName)) {
                Directory.CreateDirectory(directoryName);
            }

            File.WriteAllText(SelectionsPath, text);
        }
    }

    [GeneratedRegex("[\\/:*?\"<>|]")]
    private static partial Regex IllegalFileNameRegex { get; }

    private string SelectionsPath =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Selections",
            IllegalFileNameRegex.Replace(documentProvider.FilePath + ".sceneselections", string.Empty));

    private readonly JsonSerializerSettings _serializerSettings = new() {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        Converters = {
            JsonConvertersMixIn.FormKey,
            JsonConvertersMixIn.ModKey,
        },
    };
}
