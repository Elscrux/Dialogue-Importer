using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public class RemoveEmptyTopicInfos : IDialogueTopicProcessor {
    public void Process(DialogueTopic topic) {
        topic.TopicInfos.RemoveAll(info =>
            info.Links.Count == 0
         && info.Prompt.IsEmpty() && info.Prompt.Notes().Count == 0
         && (info.Responses.Count == 0
             || info.Responses.TrueForAll(x => x.IsEmpty() && x.Notes().Count == 0))
        );
    }
}
