using System;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class PlayerIsSexChecker : IDialogueTopicProcessor {
    [GeneratedRegex("(?:if|when)(?: player| PC)?(?: is)? (male|female)", RegexOptions.IgnoreCase)]
    private static partial Regex PlayerSexRegex { get; }

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
        var match = PlayerSexRegex.Match(note.Text);
        if (match.Success) {
            var maleFemaleGender = match.Groups[1].Value.ToLower() switch {
                "male" => MaleFemaleGender.Male,
                "female" => MaleFemaleGender.Female,
                _ => throw new InvalidOperationException(),
            };

            topicInfo.ExtraConditions.Add(
                new GetPCIsSexConditionData {
                    MaleFemaleGender = maleFemaleGender,
                }.ToConditionFloat()
            );

            return true;
        }

        return false;
    }
}
