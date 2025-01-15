using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class NullPostProcessor : IGenericDialoguePostProcessor {
    public void Process(Quest quest, DialogTopic dialogTopic) {}
}
