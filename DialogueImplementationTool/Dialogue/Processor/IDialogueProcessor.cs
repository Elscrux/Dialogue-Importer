using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public interface IDialogueProcessor {
    public DialogueResponse BuildResponse(IReadOnlyList<FormattedText> textSnippets) {
        var dialogueResponse = new DialogueResponse {
            Response = string.Join(string.Empty, textSnippets.Select(x => x.Text))
        };

        // Apply processors
        Process(dialogueResponse, textSnippets);

        return dialogueResponse;
    }

    void PreProcess(DialogueTopicInfo topicInfo);
    void PostProcess(DialogueTopicInfo topicInfo);
    void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets);
    void Process(DialogueTopic topic);
    void Process(List<DialogueTopic> topics);
    void Process(Conversation conversation);
}
