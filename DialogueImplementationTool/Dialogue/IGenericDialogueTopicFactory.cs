using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public interface IGenericDialogueTopicFactory {
    DialogTopic Create(IQuestGetter quest, DialogueTopicInfo topicInfo);
}
