using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public interface IGenericDialoguePostProcessor {
    void Process(Quest quest, DialogTopic dialogTopic);
}