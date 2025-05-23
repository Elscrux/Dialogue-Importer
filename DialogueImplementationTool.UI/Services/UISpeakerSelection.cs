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
    ISpeakerFavoritesSelection speakerFavoritesSelection,
    string filePath)
    : ISpeakerSelection {

    private Dictionary<string, Dictionary<string, AliasSelectionDto>>? _savedSceneSelections;
    private readonly AutomaticSpeakerSelection _automaticSpeakerSelection = new(linkCache, speakerFavoritesSelection);

    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames) where T : class, ISpeaker {
        var speakers = new ObservableCollection<AliasSpeakerSelection>(speakerNames
            .Select(s => new AliasSpeakerSelection(linkCache, speakerFavoritesSelection, s))
            .ToList());

        // Try load from file
        if (LoadSpeakers()) {
            return speakers
                .Select(x => ISpeakerSelection.CreateSpeaker<T>(linkCache, x.FormLink, x.Name, editorId: x.EditorID))
                .ToList();
        }

        // Try set automatically
        var automaticSpeakers = _automaticSpeakerSelection.GetSpeakers<AliasSpeaker>(speakerNames);
        foreach (var automaticSpeaker in automaticSpeakers) {
            var speaker = speakers.FirstOrDefault(s => s.Name == automaticSpeaker.Name);
            if (speaker is null) continue;

            speaker.FormKey = automaticSpeaker.FormLink.FormKey;
            speaker.EditorID = automaticSpeaker.EditorID;
        }

        new SceneSpeakerWindow(linkCache, speakerFavoritesSelection, speakers).ShowDialog();

        while (speakers.Any(s => s.FormLink.IsNull)) {
            MessageBox.Show("You must assign every speaker of the scene to an npc");
            new SceneSpeakerWindow(linkCache, speakerFavoritesSelection, speakers).ShowDialog();
        }

        SaveSpeakers(speakers);

        return speakers
            .Select(x => ISpeakerSelection.CreateSpeaker<T>(linkCache, x.FormLink, x.Name, editorId: x.EditorID))
            .ToList();

        bool LoadSpeakers() {
            _savedSceneSelections ??= LoadFromFile();
            if (_savedSceneSelections is null) return false;

            if (!_savedSceneSelections.TryGetValue(string.Join('|', speakerNames), out var savedSelections)) return false;

            foreach (var selection in speakers) {
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

        void SaveSpeakers(ObservableCollection<AliasSpeakerSelection> aliasSpeakers) {
            var selections = new Dictionary<string, AliasSelectionDto>();
            foreach (var selection in aliasSpeakers) {
                selections[selection.Name] = new AliasSelectionDto(selection.FormLink, selection.EditorID);
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
            IllegalFileNameRegex.Replace(filePath + ".sceneselections", string.Empty));

    private readonly JsonSerializerSettings _serializerSettings = new() {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        Converters = {
            JsonConvertersMixIn.FormKey,
            JsonConvertersMixIn.ModKey,
        },
    };
}
