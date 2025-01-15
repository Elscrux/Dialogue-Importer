using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class ExistingDialogueTopicFactory(DialogTopic dialogTopic) : IGenericDialogueTopicFactory {
    public DialogTopic Create(IQuestGetter quest, DialogueTopicInfo topicInfo) {
        return dialogTopic;
    }
}
