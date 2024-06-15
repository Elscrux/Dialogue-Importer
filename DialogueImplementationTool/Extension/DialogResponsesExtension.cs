using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Extension;

public static class DialogResponsesExtension {
    public static FormKey GetMainSpeaker(this IDialogResponsesGetter responses) {
        foreach (var condition in responses.Conditions) {
            if (condition is not ConditionFloat { Data: IGetIsIDConditionDataGetter getIsID }) continue;

            if (getIsID.Object.UsesLink()) {
                return getIsID.Object.Link.FormKey;
            }
        }

        return FormKey.Null;
    }
    
    public static bool IsSayOnce(this IDialogResponsesGetter responses) {
        return (responses.Flags?.Flags & DialogResponses.Flag.SayOnce) != 0;
    }
}
