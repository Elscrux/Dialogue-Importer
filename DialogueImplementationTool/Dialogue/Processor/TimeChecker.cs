using System.Linq;
using DialogueImplementationTool.Converter;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TimeChecker : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt.StartNotes.RemoveAll(CheckNote);
        topicInfo.Prompt.EndsNotes.RemoveAll(CheckNote);
        foreach (var response in topicInfo.Responses) {
            response.StartNotes.RemoveAll(CheckNote);
            response.EndsNotes.RemoveAll(CheckNote);
        }

        bool CheckNote(Note note) {
            var conditions = TimeConditionConverter.Convert(note.Text).ToList();
            if (conditions.Count == 0) return false;

            topicInfo.ExtraConditions.AddRange(conditions);

            return true;
        }
    }
}
