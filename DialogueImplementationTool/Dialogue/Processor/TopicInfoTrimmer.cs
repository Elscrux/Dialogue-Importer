using DialogueImplementationTool.Dialogue.Model;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TopicInfoTrimmer : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt.Text = topicInfo.Prompt.Text.Trim();
        topicInfo.Responses.ForEach(r => r.Response = r.Response.Trim());
        topicInfo.Responses.RemoveWhere(r => r.IsEmpty() && r.Notes().Count == 0);
    }
}
