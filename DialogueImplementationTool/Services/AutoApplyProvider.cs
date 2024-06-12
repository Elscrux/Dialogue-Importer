namespace DialogueImplementationTool.Services;

public sealed class AutoApplyProvider(bool defaultValue = true) {
    public bool AutoApply { get; set; } = defaultValue;
}
