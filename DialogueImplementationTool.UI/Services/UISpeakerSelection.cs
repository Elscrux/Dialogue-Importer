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

sealed record AliasSelectionDto(FormKey FormKey, string? EditorID);

public sealed partial class UISpeakerSelection(
    ILinkCache linkCache,
    ISpeakerFavoritesSelection speakerFavoritesSelection,
    string filePath)
    : ISpeakerSelection {
    private readonly AutomaticSpeakerSelection _automaticSpeakerSelection = new(linkCache, speakerFavoritesSelection);

    public IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IReadOnlyList<string> speakerNames) {
        var speakers = new ObservableCollection<AliasSpeakerSelection>(speakerNames
            .Select(s => new AliasSpeakerSelection(linkCache, speakerFavoritesSelection, s))
            .ToList());

        // Try load from file
        if (LoadSpeakers()) {
            return speakers
                .Select(x => new AliasSpeaker(x.FormKey, x.Name, editorId: x.EditorID))
                .ToList();
        }

        // Try set automatically
        var automaticSpeakers = _automaticSpeakerSelection.GetAliasSpeakers(speakerNames);
        foreach (var automaticSpeaker in automaticSpeakers) {
            var speaker = speakers.FirstOrDefault(s => s.Name == automaticSpeaker.Name);
            if (speaker is null) continue;

            speaker.FormKey = automaticSpeaker.FormKey;
            speaker.EditorID = automaticSpeaker.EditorID;
        }

        new SceneSpeakerWindow(linkCache, speakerFavoritesSelection, speakers).ShowDialog();

        while (speakers.Any(s => s.FormKey == FormKey.Null)) {
            MessageBox.Show("You must assign every speaker of the scene to an npc");
            new SceneSpeakerWindow(linkCache, speakerFavoritesSelection, speakers).ShowDialog();
        }

        SaveSpeakers(speakers);

        return speakers
            .Select(x => new AliasSpeaker(x.FormKey, x.Name, editorId: x.EditorID))
            .ToList();

        bool LoadSpeakers() {
            if (!File.Exists(SelectionsPath)) return false;

            var text = File.ReadAllText(SelectionsPath);
            var sceneSelections =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, AliasSelectionDto>>>(text,
                    _serializerSettings);
            if (sceneSelections is null) return false;

            foreach (var (namesString, selections) in sceneSelections) {
                var names = namesString.Split('|');
                if (!speakers.Select(x => x.Name).SequenceEqual(names)) continue;

                foreach (var selection in speakers) {
                    selection.FormKey = selections[selection.Name].FormKey;
                    selection.EditorID = selections[selection.Name].EditorID;
                }

                return true;
            }

            return false;
        }

        void SaveSpeakers(ObservableCollection<AliasSpeakerSelection> aliasSpeakers) {
            var selections = new Dictionary<string, AliasSelectionDto>();
            var sceneSelections = new Dictionary<string, Dictionary<string, AliasSelectionDto>> {
                [string.Join('|', speakerNames)] = selections,
            };
            foreach (var selection in aliasSpeakers) {
                selections[selection.Name] = new AliasSelectionDto(selection.FormKey, selection.EditorID);
            }

            var text = JsonConvert.SerializeObject(sceneSelections, _serializerSettings);
            var directoryName = Path.GetDirectoryName(SelectionsPath);
            if (directoryName is null) return;

            if (!Directory.Exists(directoryName)) {
                Directory.CreateDirectory(directoryName);
            }

            File.WriteAllText(SelectionsPath, text);
        }
    }

    [GeneratedRegex("[\\/:*?\"<>|]")]
    private static partial Regex IllegalFileNameRegex();

    private string SelectionsPath =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Selections",
            IllegalFileNameRegex().Replace(filePath + ".sceneselections", string.Empty));

    private readonly JsonSerializerSettings _serializerSettings = new() {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        Converters = {
            JsonConvertersMixIn.FormKey,
            JsonConvertersMixIn.ModKey,
        },
    };
}
