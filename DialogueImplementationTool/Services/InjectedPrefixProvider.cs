namespace DialogueImplementationTool.Services;

public sealed class InjectedPrefixProvider : IPrefixProvider {
    public string Prefix { get; set; } = string.Empty;
}
