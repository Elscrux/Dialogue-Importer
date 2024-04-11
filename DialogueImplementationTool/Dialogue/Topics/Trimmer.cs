using Noggog;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class Trimmer : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt = topicInfo.Prompt.Trim();
        topicInfo.Responses.ForEach(r => r.Response = r.Response.Trim());
        topicInfo.Responses.RemoveWhere(r => r.Response.IsNullOrEmpty());
    }
}
