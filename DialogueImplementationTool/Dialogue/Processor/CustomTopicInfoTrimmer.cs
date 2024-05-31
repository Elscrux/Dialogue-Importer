using DialogueImplementationTool.Dialogue.Model;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class CustomTopicInfoTrimmer(string trim) : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt.Text = topicInfo.Prompt.Text.TrimEnd(trim).TrimStart(trim).Trim();
        topicInfo.Responses.ForEach(r => r.Response = r.Response.TrimEnd(trim).TrimStart(trim).Trim());
        topicInfo.Responses.RemoveWhere(r => r.IsEmpty() && r.Notes().Count == 0);
    }
}
