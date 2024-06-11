using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.Services;

public sealed class AutoApplyProvider(bool defaultValue = true) {
    [Reactive] public bool AutoApply { get; set; } = defaultValue;
}
