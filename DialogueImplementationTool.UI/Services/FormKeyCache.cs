using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Services;
using Mutagen.Bethesda.Json;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Newtonsoft.Json;
using Noggog;
namespace DialogueImplementationTool.UI.Services;

public partial class FormKeyCache {
    private readonly object _writeLock = new();
    private readonly string _contextHash;
    private readonly Dictionary<string, Dictionary<string, FormKey>> _cache;

    [GeneratedRegex("[\\/:*?\"<>|]")]
    private static partial Regex IllegalFileNameRegex { get; }

    private string SelectionsPath =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Selections",
            IllegalFileNameRegex.Replace(_contextHash + ".formkeyselections", string.Empty));

    private readonly JsonSerializerSettings _serializerSettings = new() {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.Auto,
        Converters = {
            JsonConvertersMixIn.FormKey,
            JsonConvertersMixIn.ModKey,
        },
    };

    public FormKeyCache(IEnvironmentContext context) {
        var source = string.Join(", ", context.Environment.LoadOrder.ListedOrder.Select(mod => mod.ModKey.FileName.String));
        var hashData = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        var hexString = hashData.ToHexString();
        _contextHash = hexString;
        _cache = LoadSelection();
    }

    public Dictionary<string, Dictionary<string, FormKey>> LoadSelection() {
        var selectionsPath = SelectionsPath;
        if (!File.Exists(selectionsPath)) return [];

        var text = File.ReadAllText(selectionsPath);
        var selections =
            JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, FormKey>>>(text, _serializerSettings);
        return selections ?? [];
    }

    public void SaveSelection() {
        lock (_writeLock) {
            var selectionsString = JsonConvert.SerializeObject(_cache, _serializerSettings);
            var directoryName = Path.GetDirectoryName(SelectionsPath);
            if (directoryName is null) return;

            if (!Directory.Exists(directoryName)) {
                Directory.CreateDirectory(directoryName);
            }

            File.WriteAllText(SelectionsPath, selectionsString);
        }
    }

    public bool TryGetFormKey<TMajor>(string identifier, out FormKey formKey)
        where TMajor : IMajorRecordQueryableGetter {
        var fullName = typeof(TMajor).FullName;
        if (fullName != null
         && _cache.TryGetValue(fullName, out var dict)
         && dict.TryGetValue(identifier, out formKey)) {
            return true;
        }

        formKey = FormKey.Null;
        return false;
    }

    public void Set<TMajor>(string title, FormKey formKey)
        where TMajor : IMajorRecordQueryableGetter {
        var fullName = typeof(TMajor).FullName;
        if (fullName is null) throw new InvalidOperationException("Type.FullName is null");

        var dict = _cache.GetOrAdd(fullName);
        dict[title] = formKey;
        SaveSelection();
    }
}
