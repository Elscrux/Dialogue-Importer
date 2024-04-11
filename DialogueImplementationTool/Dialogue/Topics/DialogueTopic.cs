using System.Collections.Generic;
using System.Linq;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class DialogueTopic {
    public List<DialogueTopicInfo> TopicInfos { get; init; } = [];
    public bool Blocking { get; set; }

    public IEnumerable<DialogueTopic> EnumerateLinks(bool includeSelf = true) {
        if (includeSelf) yield return this;

        var returnedLinks = new HashSet<DialogueTopic>();

        var queue = new Queue<DialogueTopic>(TopicInfos.SelectMany(x => x.Links));
        while (queue.Any()) {
            var dialogueTopic = queue.Dequeue();
            if (!returnedLinks.Add(dialogueTopic)) continue;

            foreach (var topicInfo in dialogueTopic.TopicInfos) {
                foreach (var link in topicInfo.Links) {
                    queue.Enqueue(link);
                }
            }

            yield return dialogueTopic;
        }
    }

    public string GetPlayerText() {
        var prompts = TopicInfos.Select(x => x.Prompt).Distinct().ToList();

        // If there are multiple prompts, return empty string - prompts for the topics will be used instead
        if (prompts.Count > 1) return string.Empty;

        return prompts[0];
    }
}
