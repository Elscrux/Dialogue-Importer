using System.Collections.Generic;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Conversation; 

public interface IConversationProcessor {
    void Process(IList<GeneratedDialogue> dialogues);
}