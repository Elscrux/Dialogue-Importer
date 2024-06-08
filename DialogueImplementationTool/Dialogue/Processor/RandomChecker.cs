using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class RandomChecker : IDialogueTopicProcessor {
    [GeneratedRegex("(random)(ized)?", RegexOptions.IgnoreCase)]
    private static partial Regex RandomRegex();

    public void Process(DialogueTopic topic) {
        for (var i = 0; i < topic.TopicInfos.Count; i++) {
            var topicInfo = topic.TopicInfos[i];
            if (topicInfo.Responses.Count == 0) continue;

            // Check if all responses have a random tag
            if (!topicInfo.Responses.TrueForAll(x => x.Notes().Any(note => RandomRegex().IsMatch(note.Text)))) continue;

            // Remove the random tag from all responses
            foreach (var response in topicInfo.Responses) {
                response.RemoveNote(text => RandomRegex().IsMatch(text));
            }

            // Flatten the multiple random topic infos and move them into the topic as individual topic infos
            topicInfo.Random = true;
            for (var responseIndex = 1; responseIndex < topicInfo.Responses.Count; responseIndex++) {
                topic.TopicInfos.Insert(
                    i + responseIndex,
                    topicInfo.CopyWith([topicInfo.Responses[responseIndex]]));
            }

            topicInfo.Responses.RemoveRange(1, topicInfo.Responses.Count - 1);
        }
    }
}
