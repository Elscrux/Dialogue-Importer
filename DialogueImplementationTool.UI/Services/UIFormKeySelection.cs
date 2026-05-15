using System.Windows;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Services;
using DialogueImplementationTool.UI.Views;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.UI.Services;

public sealed class UIFormKeySelection(
    IEnvironmentContext environmentContext,
    AutomaticSpeakerSelection automaticSpeakerSelection,
    AutoApplyProvider autoApplyProvider,
    FormKeyCache formKeyCache)
    : IFormKeySelection {
    private readonly Lock _lock = new();
    private readonly HashSet<string> _openedIdentifiers = [];

    public FormKey GetFormKey<TMajor>(string title, string identifier, FormKey defaultFormKey, bool canBeNull = false)
        where TMajor : IMajorRecordQueryableGetter {
        // Only show one form key selection window at a time
        lock (_lock) {
            var formKey = GetFormKeyImpl(title, identifier, defaultFormKey, canBeNull, typeof(TMajor));
            formKeyCache.Set<TMajor>(identifier, formKey);
            return formKey;
        }
    }

    public FormKey GetFormKey(
        string title,
        string identifier,
        FormKey defaultFormKey,
        bool canBeNull = false,
        params IReadOnlyList<Type> recordTypes) {
        lock (_lock) {
            var formKey = GetFormKeyImpl(title, identifier, defaultFormKey, canBeNull, recordTypes);
            foreach (var recordType in recordTypes) {
                if (environmentContext.LinkCache.TryResolve(formKey, recordType, out _)) {
                    formKeyCache.Set(identifier, formKey, recordType);
                    break;
                }
            }
            return formKey;
        }
    }

    private FormKey GetFormKeyImpl(
        string title,
        string identifier,
        FormKey defaultFormKey,
        bool canBeNone,
        params IReadOnlyList<Type> recordTypes) {
        if (formKeyCache.TryGetFormKey(identifier, out var formKey, recordTypes)) {
            if (CanSkipPrompt()) return formKey;

            defaultFormKey = formKey;
        }

        if (recordTypes.Any(recordType => typeof(INpcGetter).IsAssignableFrom(recordType))) {
            var closestSpeakers = automaticSpeakerSelection
                .GetSpeakers<NpcSpeaker>([identifier], false)
                .ToArray();

            switch (closestSpeakers) {
                case [var closestSpeaker] when CanSkipPrompt():
                    return closestSpeaker.FormLink.FormKey;
                case [var closestSpeaker, ..]:
                    // Otherwise, default to the closest speaker
                    defaultFormKey = closestSpeaker.FormLink.FormKey;
                    break;
            }
        }

        formKey = Application.Current.Dispatcher.Invoke(() => {
            var formKeySelection = GetSelection();
            formKeySelection.ShowDialog();
            _openedIdentifiers.Add(identifier);

            while (formKeySelection.FormKey == FormKey.Null) {
                if (canBeNone) return FormKey.Null;

                MessageBox.Show("You must select a form key");
                formKeySelection = GetSelection();
                formKeySelection.ShowDialog();
            }

            return formKeySelection.FormKey;

            FormKeySelectionWindow GetSelection() => new(title, environmentContext.LinkCache, recordTypes, defaultFormKey);
        });

        return formKey;

        // Don't prompt if it should auto apply, or if it's already been opened before
        bool CanSkipPrompt() => autoApplyProvider.AutoApply || _openedIdentifiers.Contains(identifier);
    }
}
