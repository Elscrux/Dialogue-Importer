using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Models;
using Mutagen.Bethesda.Json;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;
namespace DialogueImplementationTool.UI.Services;

using SceneSelections = Dictionary<string, Dictionary<string, AliasSelectionDto>>;

sealed record AliasSelectionDto(FormKey FormKey, string? EditorID);

public sealed partial class SpeakerSelectionRepository(
    ILinkCache linkCache,
    ISpeakerFavoritesSelection speakerFavoritesSelection,
    IDocumentProvider documentProvider)
    : ISpeakerSelection {

    private SceneSelections? _savedSceneSelections;

    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames)
        where T : class, ISpeaker {
        var speakerSelections = new ObservableCollection<AliasSpeakerSelection>(speakerNames
            .Select(s => new AliasSpeakerSelection(linkCache, speakerFavoritesSelection, s))
            .ToList());

        // Try load from file
        if (LoadSpeakers()) {
            return speakerSelections
                .Select(x => ISpeakerSelection.CreateSpeaker<T>(linkCache, new FormLinkInformation(x.FormKey, typeof(INpcGetter)), x.Name, editorId: x.EditorID))
                .ToList();
        }

        return [];

        bool LoadSpeakers() {
            _savedSceneSelections ??= LoadFromFile();
            if (_savedSceneSelections is null) return false;

            var key = string.Join('|', speakerNames.OrderBy(x => x));

            if (!_savedSceneSelections.TryGetValue(key, out var savedSelections)) return false;

            foreach (var selection in speakerSelections) {
                if (!savedSelections.TryGetValue(selection.Name, out var aliasSelectionDto)) {
                    foreach (var savedKey in savedSelections.Keys) {
                        Debug.WriteLine($"  - '{savedKey}'");
                    }
                    return false;
                }

                selection.FormKey = aliasSelectionDto.FormKey;
                selection.EditorID = aliasSelectionDto.EditorID;
            }

            return true;
        }

        SceneSelections? LoadFromFile() {
            if (!File.Exists(SelectionsPath)) return null;

            try {
                var text = File.ReadAllText(SelectionsPath);

                return JsonConvert.DeserializeObject<SceneSelections>(text, _serializerSettings);
            } catch (Exception ex) {
                Debug.WriteLine($"ERROR: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Delete corrupted file so it can be recreated
                try {
                    File.Delete(SelectionsPath);
                    Debug.WriteLine($"Deleted corrupted file: {SelectionsPath}");
                } catch {
                    Debug.WriteLine("Could not delete corrupted file");
                }

                return null;
            }
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
        TypeNameHandling = TypeNameHandling.None,
        Converters = {
            JsonConvertersMixIn.FormKey,
            JsonConvertersMixIn.ModKey,
        },
    };
}
