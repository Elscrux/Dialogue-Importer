using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class BleedOutChecker : IDialogueTopicInfoProcessor {
    [GeneratedRegex("(if )?in bleedout( state)?", RegexOptions.IgnoreCase)]
    private static partial Regex BleedOutRegex { get; }

    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt.StartNotes.RemoveAll(CheckNote);
        topicInfo.Prompt.EndsNotes.RemoveAll(CheckNote);
        foreach (var response in topicInfo.Responses) {
            response.StartNotes.RemoveAll(CheckNote);
            response.EndsNotes.RemoveAll(CheckNote);
        }

        bool CheckNote(Note note) {
            var match = BleedOutRegex.Match(note.Text);
            if (!match.Success) return false;

            topicInfo.ExtraConditions.Add(
                new IsBleedingOutConditionData().ToConditionFloat()
            );

            return true;
        }
    }
}
