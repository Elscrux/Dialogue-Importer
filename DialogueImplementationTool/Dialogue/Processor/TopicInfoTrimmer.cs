using DialogueImplementationTool.Dialogue.Model;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TopicInfoTrimmer : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt = topicInfo.Prompt.Trim();
        topicInfo.Responses.ForEach(r => r.Response = r.Response.Trim());
        topicInfo.Responses.RemoveWhere(r => r.Response.IsNullOrEmpty());
    }
}
