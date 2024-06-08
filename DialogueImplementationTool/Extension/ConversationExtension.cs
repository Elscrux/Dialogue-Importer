using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Noggog;
namespace DialogueImplementationTool.Extension;

using KeywordMatch = (Note Note, string Keyword, DialogueTopic Topic, DialogueTopicInfo TopicInfo);

public static class ConversationExtension {
    public static Dictionary<string, KeywordMatch> GetKeywordTopicInfoDictionary(
        this Conversation conversation,
        Func<DialogueTopicInfo, IEnumerable<(Note Note, string Keyword)>> getKeyword) {
        var keywordDictionary = new Dictionary<string, KeywordMatch>();

        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics.SelectMany(x => x.EnumerateLinks(true))) {
                foreach (var info in topic.TopicInfos) {
                    if (info.Responses.Count == 0) continue;

                    foreach (var (note, keyword) in getKeyword(info)) {
                        if (!keywordDictionary.TryAdd(keyword, (note, keyword, topic, info))) {
                            Console.WriteLine(
                                $"Destination keyword {keyword} already exists in dialogue {topic.TopicInfos[0].Prompt.FullText}");
                        }
                    }
                }
            }
        }

        return keywordDictionary;
    }

    public static Dictionary<string, KeywordMatch> GetKeywordTopicInfoDictionary(
        this Conversation conversation,
        Regex regex,
        Func<DialogueTopicInfo, IEnumerable<Note>> getNotes) {
        return conversation.GetKeywordTopicInfoDictionary(info => GetKeyword(getNotes(info), regex));
    }

    public static List<KeywordMatch> GetAllKeywordTopicInfos(
        this Conversation conversation,
        Func<DialogueTopicInfo, IEnumerable<(Note Note, string Keyword)>> getKeyword) {
        var list = new List<KeywordMatch>();

        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics.SelectMany(x => x.EnumerateLinks(true))) {
                foreach (var info in topic.TopicInfos) {
                    foreach (var (note, keyword) in getKeyword(info)) {
                        list.Add((note, keyword, topic, info));
                    }
                }
            }
        }

        return list;
    }

    public static List<KeywordMatch> GetAllKeywordTopicInfos(
        this Conversation conversation,
        Regex regex,
        Func<DialogueTopicInfo, IEnumerable<Note>> getNotes) {
        return conversation.GetAllKeywordTopicInfos(info => GetKeyword(getNotes(info), regex));
    }

    private static IEnumerable<(Note Note, string Keyword)> GetKeyword(IEnumerable<Note> notes, Regex regex) {
        return notes
            .SelectWhere(note => {
                var match = regex.Match(note.Text);
                if (!match.Success) return TryGet<(Note Note, string Keyword)>.Failure;

                return TryGet<(Note Note, string Keyword)>.Succeed((note, match.Groups[1].Value));
            });
    }
}
