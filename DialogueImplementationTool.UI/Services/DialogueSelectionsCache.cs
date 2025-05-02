using System.IO;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.Json;
using Newtonsoft.Json;
namespace DialogueImplementationTool.UI.Services;

public sealed partial class DialogueSelectionsCache(string documentFilePath) {
    [GeneratedRegex("[\\/:*?\"<>|]")]
    private static partial Regex IllegalFileNameRegex { get; }

    private string SelectionsPath =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Selections",
            IllegalFileNameRegex.Replace(documentFilePath + ".selections", string.Empty));

    private readonly JsonSerializerSettings _serializerSettings = new() {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        Converters = {
            JsonConvertersMixIn.FormKey,
            JsonConvertersMixIn.ModKey,
        },
    };

    public List<DialogueSelection> LoadSelection() {
        var selectionsPath = SelectionsPath;
        if (!File.Exists(selectionsPath)) return [];

        var text = File.ReadAllText(selectionsPath);
        var selections = JsonConvert.DeserializeObject<List<DialogueSelection>>(text, _serializerSettings);
        return selections ?? [];
    }

    public void SaveSelection(List<DialogueSelection> selections) {
        var selectionsString = JsonConvert.SerializeObject(selections, _serializerSettings);
        var directoryName = Path.GetDirectoryName(SelectionsPath);
        if (directoryName is null) return;

        if (!Directory.Exists(directoryName)) {
            Directory.CreateDirectory(directoryName);
        }

        File.WriteAllText(SelectionsPath, selectionsString);
    }
}
