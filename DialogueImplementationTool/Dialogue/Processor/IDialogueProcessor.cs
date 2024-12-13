using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public interface IDialogueProcessor {
    DialogueResponse BuildResponse(IReadOnlyList<FormattedText> textSnippets) {
        var dialogueResponse = new DialogueResponse {
            Response = string.Join(string.Empty, textSnippets.Select(x => x.Text)),
        };

        // Apply processors
        Process(dialogueResponse, textSnippets);

        return dialogueResponse;
    }

    void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo);
    void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets);
    void Process(DialogueTopicInfo topicInfo);
    void Process(DialogueTopic topic);
    void Process(List<DialogueTopic> topics);
    void Process(Conversation conversation);
}
