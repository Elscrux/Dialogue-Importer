using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class BelongsToPreviousChecker : IDialogueTopicProcessor {
    [GeneratedRegex("belongs to previous( line)?", RegexOptions.IgnoreCase)]
    private static partial Regex PreviousRegex { get; }

    public void Process(DialogueTopic topic) {
        var i = 1;
        while (i < topic.TopicInfos.Count) {
            var topicInfo = topic.TopicInfos[i];
            if (topicInfo.Responses.Count == 0) {
                i++;
                continue;
            }

            // Check if start note has a belongs to previous tag
            var previousNote = topicInfo.Responses[0].StartNotes.FirstOrDefault(n => PreviousRegex.IsMatch((string) n.Text));
            if (previousNote is null) {
                i++;
                continue;
            }

            // Remove note from the start notes
            topicInfo.Responses[0].StartNotes.Remove(previousNote);

            // Add the responses to the previous topic info
            var previousTopicInfo = topic.TopicInfos[i - 1];
            previousTopicInfo.Responses.AddRange(topicInfo.Responses);
            topic.TopicInfos.RemoveAt(i);
        }
    }
}
