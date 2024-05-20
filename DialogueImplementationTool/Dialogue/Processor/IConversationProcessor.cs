using System.Collections.Generic;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public interface IConversationProcessor {
    void Process(Conversation conversation);
}
