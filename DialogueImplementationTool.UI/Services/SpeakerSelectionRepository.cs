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

internal sealed record AliasSelectionDto(FormKey FormKey, string? EditorID);

public sealed partial class SpeakerSelectionRepository(
    ILinkCache linkCache,
    ISpeakerFavoritesSelection speakerFavoritesSelection,
    IDocumentProvider documentProvider)
    : ISpeakerSelection, ISpeakerSelectionRepository {

    [GeneratedRegex("[\\/:*?\"<>|]")]
    private static partial Regex IllegalFileNameRegex { get; }

    private string SelectionsPath { get; } = Path.Combine(
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

    private SceneSelections? _savedSceneSelections;

    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames)
        where T : class, ISpeaker {
        if (TryLoadSceneSpeakers(speakerNames, out var speakerSelections)) {
            return speakerSelections
                .Select(x => ISpeakerSelection.CreateSpeaker<T>(linkCache,
                    new FormLinkInformation(x.FormKey, typeof(INpcGetter)),
                    x.Name,
                    editorId: x.EditorID))
                .ToList();
        }

        return [];
    }

    public void SaveSceneSpeakers(IReadOnlyList<AliasSpeakerSelection> speakers) {
        try {
            var speakerNames = speakers.Select(s => s.Name).OrderBy(x => x).ToList();

            if (File.Exists(SelectionsPath)) {
                var json = File.ReadAllText(SelectionsPath);
                _savedSceneSelections = JsonConvert.DeserializeObject<SceneSelections>(json, _serializerSettings)
                 ?? new SceneSelections();
            } else {
                _savedSceneSelections = new SceneSelections();
            }

            var key = GetSceneKey(speakerNames);
            var selections = new Dictionary<string, AliasSelectionDto>();
            foreach (var speaker in speakers) {
                selections[speaker.Name] = new AliasSelectionDto(speaker.FormKey, speaker.EditorID);
                speakerFavoritesSelection.AddSpeaker(new NpcSpeaker(linkCache, speaker.FormLink));
            }

            _savedSceneSelections[key] = selections;

            var directory = Path.GetDirectoryName(SelectionsPath);
            if (directory != null && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var outputJson = JsonConvert.SerializeObject(_savedSceneSelections, _serializerSettings);
            File.WriteAllText(SelectionsPath, outputJson);
        } catch (Exception ex) {
            Debug.WriteLine($"ERROR: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public bool TryLoadSceneSpeakers(IReadOnlyList<string> speakerNames, out List<AliasSpeakerSelection> speakerSelections) {
        speakerSelections = [];
        try {
            if (!File.Exists(SelectionsPath)) return false;

            var json = File.ReadAllText(SelectionsPath);
            if (_savedSceneSelections is null) {
                _savedSceneSelections = JsonConvert.DeserializeObject<SceneSelections>(json, _serializerSettings); 
                if (_savedSceneSelections is null) return false;
            }

            var key = GetSceneKey(speakerNames);
            if (!_savedSceneSelections.TryGetValue(key, out var selections)) return false;

            foreach (var speakerName in speakerNames) {
                if (!selections.TryGetValue(speakerName, out var dto)) return false;

                var aliasSpeaker = new AliasSpeakerSelection(linkCache, speakerFavoritesSelection, speakerName) {
                    FormKey = dto.FormKey,
                    EditorID = dto.EditorID
                };
                speakerSelections.Add(aliasSpeaker);
            }

            return true;
        } catch (Exception ex) {
            Debug.WriteLine($"ERROR: {ex.Message}");
            return false;
        }
    }

    private static string GetSceneKey(IReadOnlyList<string> speakerNames) {
        return string.Join('|', speakerNames.OrderBy(x => x));
    }
}
