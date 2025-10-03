using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Services;

public interface IFormKeySelection {
    FormKey GetFormKey<TMajor>(string title, string identifier, FormKey defaultFormKey)
        where TMajor : IMajorRecordQueryableGetter;
}
