using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class PlayerIsVampireChecker : IDialogueTopicProcessor {
    [GeneratedRegex("(?:if|when)(?: player| PC)?(?: is)?( not)?(?: a)? vampire", RegexOptions.IgnoreCase)]
    private static partial Regex PlayerVampireRegex { get; }

    public void Process(DialogueTopic topic) {
        foreach (var topicInfo in topic.TopicInfos) {
            foreach (var note in topicInfo.Prompt.Notes()) {
                if (CheckNote(topicInfo, note)) {
                    topicInfo.Prompt.RemoveNote(note);
                }
            }

            foreach (var note in topicInfo.AllNotes()) {
                if (CheckNote(topicInfo, note)) {
                    foreach (var response in topicInfo.Responses) {
                        response.RemoveNote(note);
                    }
                }
            }
        }
    }

    public bool CheckNote(DialogueTopicInfo topicInfo, Note note) {
        var match = PlayerVampireRegex.Match(note.Text);
        if (match.Success) {
            var not = match.Groups[1].Value.ToLower() switch {
                "not" => true,
                _ => false,
            };

            topicInfo.ExtraConditions.Add(new ConditionFloat {
                Data = new GetGlobalValueConditionData {
                    Global = { Link = { FormKey = Skyrim.Global.PlayerIsVampire.FormKey } },
                },
                ComparisonValue = not ? 0 : 1,
                CompareOperator = CompareOperator.EqualTo,
            });

            return true;
        }

        return false;
    }
}
