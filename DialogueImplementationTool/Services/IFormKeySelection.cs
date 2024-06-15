using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Services;

public interface IFormKeySelection {
    FormKey GetFormKey<TMajor>(string title, FormKey defaultFormKey) where TMajor : IMajorRecordQueryableGetter;
}
