using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
namespace DialogueImplementationTool.Services;

public class InjectedFormKeySelection : IFormKeySelection {
    public FormKey GetFormKey<TMajor>(string title, FormKey defaultFormKey) 
        where TMajor : IMajorRecordQueryableGetter => defaultFormKey;
}
