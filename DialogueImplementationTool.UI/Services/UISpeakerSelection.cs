using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Models;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Json;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;
namespace DialogueImplementationTool.UI.Services;

sealed record AliasSelectionDto(FormKey FormKey, string? EditorID);

public sealed partial class UISpeakerSelection(
    ILinkCache linkCache,
    ISpeakerFavoritesSelection speakerFavoritesSelection,
    string filePath)
    : ISpeakerSelection {
    public IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IReadOnlyList<string> speakerNames) {
        var speakers = new ObservableCollection<AliasSpeakerSelection>(speakerNames
            .Select(s => new AliasSpeakerSelection(linkCache, speakerFavoritesSelection, s))
            .ToList());

        if (LoadSpeakers()) {
            return speakers
                .Select(x => new AliasSpeaker(x.FormKey, x.Name, editorId: x.EditorID))
                .ToList();
        }

        TryMatchFromLoadOrder(speakers);

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

    private void TryMatchFromLoadOrder(ObservableCollection<AliasSpeakerSelection> speakers) {
        foreach (var speaker in speakers) {
            if (speaker.Name.Length < 4) continue;

            var count = 0;
            INpcGetter? currentNpc = null;
            foreach (var npc in linkCache.PriorityOrder.WinningOverrides<INpcGetter>()) {
                if (npc.EditorID is null) continue;
                if (!npc.EditorID.Contains(speaker.Name, StringComparison.Ordinal)) continue;

                count++;
                if (count > 1) break;

                currentNpc = npc;
            }

            if (count == 1 && currentNpc is not null) {
                speaker.FormKey = currentNpc.FormKey;
                speaker.EditorID = currentNpc.EditorID;
            }
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
