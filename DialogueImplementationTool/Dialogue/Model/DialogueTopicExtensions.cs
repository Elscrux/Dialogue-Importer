using System.Collections.Generic;
using System.Linq;
namespace DialogueImplementationTool.Dialogue.Model;

public static class DialogueTopicExtensions {
    public static IEnumerable<DialogueTopic> EnumerateLinks(this IEnumerable<DialogueTopic> topics, bool includeSelf) {
        return topics.SelectMany(topic => topic.EnumerateLinks(includeSelf)).Distinct();
    }

    /// <summary>
    ///     Through shared dialogue detection, a topic that was previously only
    ///     one topic might be split into multiple topics.
    ///     This is basically flattening the dialogue tree.
    /// </summary>
    /// <param name="topics">Topics tree</param>
    /// <returns>Flattened topics list</returns>
    public static List<DialogueTopicInfo> ToTopicInfoList(this IEnumerable<DialogueTopic> topics) {
        var topicList = topics.ToList();
        var browsedLinks = new HashSet<DialogueTopic>();
        var allTopics = new List<DialogueTopic>();

        foreach (var topic in topicList.EnumerateLinks(true)) {
            var indexOf = allTopics.IndexOf(topic);
            allTopics.Insert(indexOf + 1, topic);
            if (!browsedLinks.Add(topic)) continue;

            foreach (var topicInfo in topic.TopicInfos) {
                for (var i = topicInfo.Links.Count - 1; i >= 0; i--) allTopics.Insert(indexOf + 1, topicInfo.Links[i]);
            }
        }

        return allTopics
            .SelectMany(x => x.TopicInfos)
            .Distinct()
            .ToList();
    }
}
