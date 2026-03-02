using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Models;
using DialogueImplementationTool.UI.Views;
using Loqui;
using Mutagen.Bethesda.Json;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Newtonsoft.Json;
using Noggog;
using Activator = System.Activator;
namespace DialogueImplementationTool.UI.Services;

public sealed class FormKeyJsonConverter : JsonConverter {
    public override bool CanConvert(Type objectType) {
        if (objectType == typeof(FormKey)) return true;
        if (objectType == typeof(FormKey?)) return true;
        if (objectType == typeof(FormLinkInformation)) return true;
        if (typeof(IFormLinkGetter).IsAssignableFrom(objectType)) return true;
        return false;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
        var obj = reader.Value;
        if (obj == null) {
            if (objectType == typeof(FormKey)) {
                return FormKey.Null;
            }
            if (objectType == typeof(FormKey?)) {
                return null;
            }
            if (!objectType.Name.Contains("FormLink")) {
                throw new ArgumentException($"Unexpected null value for type {objectType.Name}");
            }

            if (objectType.IsGenericType && objectType.GenericTypeArguments.Length > 0) {
                if (IsNullableLink(objectType)) {
                    return Activator.CreateInstance(
                        typeof(FormLinkNullable<>).MakeGenericType(objectType.GenericTypeArguments[0]));
                } else {
                    return Activator.CreateInstance(
                        typeof(FormLink<>).MakeGenericType(objectType.GenericTypeArguments[0]),
                        FormKey.Null);
                }
            }

            // Fallback to FormLinkInformation for non-generic or untyped FormLinks
            return new FormLinkInformation(FormKey.Null, typeof(IMajorRecordGetter));
        } else {
            var str = obj.ToString();

            if (objectType == typeof(FormKey)) {
                if (str.IsNullOrWhitespace()) {
                    return FormKey.Null;
                } else {
                    return FormKey.Factory(str);
                }
            } else if (objectType == typeof(FormKey?)) {
                if (str.IsNullOrWhitespace()) {
                    return default(FormKey?);
                } else {
                    return FormKey.Factory(str);
                }
            }

            if (!objectType.Name.Contains("FormLink")) {
                throw new ArgumentException($"Unexpected type {objectType.Name} for FormKey deserialization");
            }

            if (objectType.IsGenericType && objectType.GenericTypeArguments.Length > 0) {
                FormKey key;
                if (str.IsNullOrWhitespace()) {
                    key = FormKey.Null;
                    return GetFormLink(objectType.GenericTypeArguments[0], key);
                }

                (key, var regis) = ParseFormKeyAndType(str);

                var type = objectType.GenericTypeArguments[0];

                return GetFormLink(type, key);
            } else {
                if (str.IsNullOrWhitespace() || str == "Null") {
                    return new FormLinkInformation(FormKey.Null, typeof(IMajorRecordGetter));
                }

                var (key, regis) = ParseFormKeyAndType(str);

                return new FormLinkInformation(
                    key,
                    regis.GetterType);
            }
        }

        (FormKey FormKey, ILoquiRegistration Registration) ParseFormKeyAndType(string str) {
            var span = str.AsSpan();
            var startIndex = span.IndexOf('<');
            if (startIndex == -1) {
                throw new ArgumentException($"Invalid FormKey format (missing '<'): {str}");
            }

            var endIndex = span.IndexOf('>');
            if (endIndex == -1) {
                throw new ArgumentException($"Invalid FormKey format (missing '>'): {str}");
            }

            var key = FormKey.Factory(span[..startIndex]);
            var typeName = span.Slice(startIndex + 1, endIndex - 1 - startIndex).ToString();

            var lastPeriod = typeName.LastIndexOf('.');
            if (lastPeriod != -1 && typeName[(lastPeriod + 1)..] == "MajorRecord") {
                typeName = "Mutagen.Bethesda.Plugins.Records.MajorRecord";
            } else if (!typeName.StartsWith("Mutagen.Bethesda.")) {
                typeName = "Mutagen.Bethesda." + typeName;
            }
            var regis = LoquiRegistration.GetRegisterByFullName(typeName);
            if (regis == null) {
                throw new ArgumentException($"Unknown object type: {typeName}");
            }

            return (key, regis);
        }

        object? GetFormLink(Type type, FormKey key) {
            if (IsNullableLink(objectType)) {
                return Activator.CreateInstance(
                    typeof(FormLinkNullable<>).MakeGenericType(type),
                    key);
            } else {
                return Activator.CreateInstance(
                    typeof(FormLink<>).MakeGenericType(type),
                    key);
            }
        }
    }

    private bool IsNullableLink(Type type) {
        return type.Name.Length > 2 && type.Name.AsSpan()[..^2].EndsWith("Nullable", StringComparison.Ordinal);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        if (value == null) return;
        switch (value) {
            case FormKey fk:
                writer.WriteValue(fk.ToString());
                break;
            case IFormLinkIdentifier ident:
                writer.WriteValue(IFormLinkIdentifier.GetString(ident, simpleType: true));
                break;
            default:
                throw new ArgumentException($"Cannot serialize type {value.GetType().Name}");
        }
    }
}

sealed record AliasSelectionDto(FormKey FormKey, string? EditorID);

public sealed partial class UISpeakerSelection(
    ILinkCache linkCache,
    AutomaticSpeakerSelection automaticSpeakerSelection,
    ISpeakerFavoritesSelection speakerFavoritesSelection,
    IDocumentProvider documentProvider)
    : ISpeakerSelection {

    private Dictionary<string, Dictionary<string, AliasSelectionDto>>? _savedSceneSelections;

    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames)
        where T : class, ISpeaker {
        var speakerSelections = new ObservableCollection<AliasSpeakerSelection>(speakerNames
            .Select(s => new AliasSpeakerSelection(linkCache, speakerFavoritesSelection, s))
            .ToList());

        // Try load from file
        if (LoadSpeakers()) {
            var loadedSpeakers = speakerSelections
                .Select(x => ISpeakerSelection.CreateSpeaker<T>(linkCache, new FormLinkInformation(x.FormKey, typeof(INpcGetter)), x.Name, editorId: x.EditorID))
                .ToList();
            return loadedSpeakers;
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
            var speaker = speakerSelections.FirstOrDefault(s =>
                s.Name == automaticSpeaker.NameNoSpaces || s.Name == automaticSpeaker.Name);
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
            if (_savedSceneSelections is null) {
                return false;
            }

            var key = string.Join('|', speakerNames.OrderBy(x => x));

            if (!_savedSceneSelections.TryGetValue(key, out var savedSelections)) {
                return false;
            }

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

        Dictionary<string, Dictionary<string, AliasSelectionDto>>? LoadFromFile() {
            if (!File.Exists(SelectionsPath)) {
                return null;
            }

            try {
                var text = File.ReadAllText(SelectionsPath);

                var sceneSelections =
                    JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, AliasSelectionDto>>>(text,
                        _serializerSettings);

                return sceneSelections;
            } catch (Exception ex) {
                Debug.WriteLine($"ERROR: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Delete corrupted file so it can be recreated
                try {
                    File.Delete(SelectionsPath);
                    Debug.WriteLine($"Deleted corrupted file: {SelectionsPath}");
                } catch {
                    Debug.WriteLine($"Could not delete corrupted file");
                }

                return null;
            }
        }

        void SaveSpeakers(IReadOnlyList<ISpeaker> aliasSpeakers) {
            var selections = new Dictionary<string, AliasSelectionDto>();
            foreach (var speaker in aliasSpeakers) {
                selections[speaker.Name] = new AliasSelectionDto(speaker.FormLink.FormKey, speaker.EditorID);
                speakerFavoritesSelection.AddSpeaker(speaker);
            }

            _savedSceneSelections ??= LoadFromFile();
            _savedSceneSelections ??= new Dictionary<string, Dictionary<string, AliasSelectionDto>>();

            var key = string.Join('|', speakerNames);
            _savedSceneSelections[key] = selections;

            try {
                var text = JsonConvert.SerializeObject(_savedSceneSelections, _serializerSettings);
                var directoryName = Path.GetDirectoryName(SelectionsPath);
                if (directoryName is null) return;

                if (!Directory.Exists(directoryName)) {
                    Directory.CreateDirectory(directoryName);
                }

                File.WriteAllText(SelectionsPath, text);
            } catch (Exception ex) {
                Debug.WriteLine($"ERROR saving file: {ex.Message}");
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
            new FormKeyJsonConverter(),
            JsonConvertersMixIn.ModKey,
        },
    };
}