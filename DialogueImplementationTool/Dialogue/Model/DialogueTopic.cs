using System.Collections.Generic;
using System.Linq;
namespace DialogueImplementationTool.Dialogue.Model;

public sealed class DialogueTopic {
    public List<DialogueTopicInfo> TopicInfos { get; init; } = [];
    public bool Blocking { get; set; }

    public IEnumerable<DialogueTopic> EnumerateLinks(bool includeSelf) {
        if (includeSelf) yield return this;

        var returnedLinks = new HashSet<DialogueTopic>();

        var queue = new Queue<DialogueTopic>(TopicInfos.SelectMany(x => x.Links));
        while (queue.Count != 0) {
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

    public void ConvertResponsesToTopicInfos() {
        var newInfos = new List<DialogueTopicInfo>();

        foreach (var topicInfo in TopicInfos) {
            foreach (var response in topicInfo.Responses) {
                newInfos.Add(topicInfo.CopyWith([response]));
            }
        }

        TopicInfos.Clear();
        TopicInfos.AddRange(newInfos);
    }
}