using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Dialogue.Speaker;

public sealed class AliasSpeaker(FormKey formKey, string name, int aliasIndex = -1, string? editorId = null)
    : ISpeaker {
    public int AliasIndex { get; set; } = aliasIndex;
    public string Name { get; } = ISpeaker.GetSpeakerName(name);
    public FormKey FormKey { get; } = formKey;
    public string? EditorID { get; } = editorId;
}