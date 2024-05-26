using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Model;

[DebuggerDisplay("{ToString()}")]
public sealed class DialogueTopic : IEquatable<DialogueTopic> {
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

    public string GetPlayerFullText() {
        var prompts = TopicInfos.Select(x => x.Prompt.FullText).Distinct().ToList();

        if (prompts.Count == 1) return prompts[0];

        // If there are multiple prompts, return empty string - prompts for the topics will be used instead
        return string.Empty;
    }

    public string GetPlayerText() {
        var prompts = TopicInfos.Select(x => x.Prompt.Text).Distinct().ToList();

        if (prompts.Count == 1) return prompts[0];

        // If there are multiple prompts, return empty string - prompts for the topics will be used instead
        return string.Empty;
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

    public override string ToString() {
        var playerText = GetPlayerFullText();
        if (!string.IsNullOrEmpty(playerText)) return playerText;

        return $"Topic with {TopicInfos.Count} prompts";
    }

    public bool EqualsNoLinks(DialogueTopic? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Blocking == other.Blocking
         && TopicInfos.Count == other.TopicInfos.Count
         && TopicInfos.WithIndex().All(x => x.Item.EqualsNoLinks(other.TopicInfos[x.Index]));
    }

    public bool Equals(DialogueTopic? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Blocking == other.Blocking
         && TopicInfos.SequenceEqual(other.TopicInfos);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not DialogueTopic other) return false;

        return Equals(other);
    }

    public override int GetHashCode() => HashCode.Combine(TopicInfos, Blocking);
}
