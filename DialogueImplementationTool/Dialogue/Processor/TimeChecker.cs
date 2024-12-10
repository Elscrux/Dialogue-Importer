using System.Linq;
using DialogueImplementationTool.Converter;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TimeChecker : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            foreach (var note in response.Notes()) {
                var conditions = TimeConditionConverter.Convert(note.Text).ToList();
                if (conditions.Count == 0) continue;

                topicInfo.ExtraConditions.AddRange(conditions);

                response.RemoveNote(note);
            }
        }
    }
}
