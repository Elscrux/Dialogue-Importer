using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Extension;

using KeywordLink = (string Keyword, DialogueTopic Topic, DialogueTopicInfo TopicInfo);

public static class ConversationExtension {
    public static Dictionary<string, KeywordLink> GetKeywordTopicInfoDictionary(
        this Conversation conversation,
        Func<DialogueTopicInfo, IEnumerable<string>> getKeyword) {
        var keywordDictionary = new Dictionary<string, KeywordLink>();

        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics.SelectMany(x => x.EnumerateLinks(true))) {
                foreach (var info in topic.TopicInfos) {
                    if (info.Responses.Count == 0) continue;

                    foreach (var keyword in getKeyword(info)) {
                        if (!keywordDictionary.TryAdd(keyword, (keyword, topic, info))) {
                            Console.WriteLine(
                                $"Destination keyword {keyword} already exists in dialogue {topic.TopicInfos[0].Prompt.FullText}");
                        }
                    }
                }
            }
        }

        return keywordDictionary;
    }

    public static Dictionary<string, KeywordLink> GetKeywordTopicInfoDictionary(
        this Conversation conversation,
        Regex regex,
        Func<DialogueTopicInfo, IEnumerable<Note>> getNotes) {
        return conversation.GetKeywordTopicInfoDictionary(info => GetKeyword(getNotes(info), regex));
    }

    public static List<KeywordLink> GetAllKeywordTopicInfos(
        this Conversation conversation,
        Func<DialogueTopicInfo, IEnumerable<string>> getKeyword) {
        var list = new List<KeywordLink>();

        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics.SelectMany(x => x.EnumerateLinks(true))) {
                foreach (var info in topic.TopicInfos) {
                    if (info.Responses.Count == 0) continue;

                    foreach (var keyword in getKeyword(info)) {
                        list.Add((keyword, topic, info));
                    }
                }
            }
        }

        return list;
    }

    public static List<KeywordLink> GetAllKeywordTopicInfos(
        this Conversation conversation,
        Regex regex,
        Func<DialogueTopicInfo, IEnumerable<Note>> getNotes) {
        return conversation.GetAllKeywordTopicInfos(info => GetKeyword(getNotes(info), regex));
    }

    private static IEnumerable<string> GetKeyword(IEnumerable<Note> notes, Regex regex) {
        return notes
            .Select(note => regex.Match(note.Text))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value);
    }
}
