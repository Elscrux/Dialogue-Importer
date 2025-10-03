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

    public FormKey GetFormKey<TMajor>(string title, string identifier, FormKey defaultFormKey)
        where TMajor : IMajorRecordQueryableGetter {
        // Only show one form key selection window at a time
        lock (_lock) {
            var formKey = GetFormKeyImpl<TMajor>(title, identifier, defaultFormKey);
            formKeyCache.Set<TMajor>(identifier, formKey);
            return formKey;
        }
    }

    private FormKey GetFormKeyImpl<TMajor>(string title, string identifier, FormKey defaultFormKey)
        where TMajor : IMajorRecordQueryableGetter {
        if (formKeyCache.TryGetFormKey<TMajor>(identifier, out var formKey)) {
            if (CanSkipPrompt()) return formKey;

            defaultFormKey = formKey;
        }

        if (typeof(INpcGetter).IsAssignableFrom(typeof(TMajor))) {
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
                MessageBox.Show("You must select a form key");
                formKeySelection = GetSelection();
                formKeySelection.ShowDialog();
            }

            return formKeySelection.FormKey;

            FormKeySelectionWindow GetSelection() => new(title, environmentContext.LinkCache, [typeof(TMajor)], defaultFormKey);
        });

        return formKey;

        // Don't prompt if it should auto apply, or if it's already been opened before
        bool CanSkipPrompt() => autoApplyProvider.AutoApply || _openedIdentifiers.Contains(identifier);
    }
}
