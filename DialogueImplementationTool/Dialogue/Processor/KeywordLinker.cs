using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

using KeywordLink = (string Keyword, DialogueTopic Topic, DialogueTopicInfo TopicInfo);

public sealed partial class KeywordLinker : IConversationProcessor {
    private const string FillerRegexPart = @"[^\]]*";
    private const string MergeRegexPart = "(?:merge|go|back)";
    private const string OptionsAfterRegexPart = "(?:options after )";
    private const string KeywordRegexPart = "([A-Z]+)";

    // [DONE], [HERE]
    [GeneratedRegex($"{KeywordRegexPart}")]
    private static partial Regex SimpleKeywordRegex();

    // [merge to DONE above]
    [GeneratedRegex($"{FillerRegexPart}{MergeRegexPart} to {KeywordRegexPart}{FillerRegexPart}")]
    private static partial Regex LinkSimpleRegex();

    // [merge to options after HERE above]
    [GeneratedRegex(
        $"{FillerRegexPart}{MergeRegexPart} to {OptionsAfterRegexPart}?{KeywordRegexPart}{FillerRegexPart}")]
    private static partial Regex LinkOptionsRegex();

    public void Process(Conversation conversation) {
        ProcessKeywordLinks(conversation);
        // todo support links to the middle of dialogue (create shared infos and so on)
        ProcessOptionLinks(conversation);
    }

    private static void ProcessOptionLinks(Conversation conversation) {
        var optionsDestinations =
            GetKeywordTopicInfoDictionary(conversation,
                info => GetKeyword(info.Responses[^1].EndsNotes, SimpleKeywordRegex()));
        var optionsLinks = GetAllKeywordTopicInfo(conversation,
            info => GetKeyword(info.Responses[^1].EndsNotes, LinkOptionsRegex()));

        foreach (var (keyword, _, linkTopicInfo) in optionsLinks) {
            if (linkTopicInfo.Links.Count > 0) {
                Console.WriteLine($"Keyword {keyword} already has links in dialogue {linkTopicInfo.Prompt}");
                continue;
            }

            if (!optionsDestinations.TryGetValue(keyword, out var destination)) {
                Console.WriteLine($"Keyword {keyword} does not have a destination in any dialogue");
                continue;
            }

            destination.TopicInfo.Responses[^1].RemoveNote(x => SimpleKeywordRegex().IsMatch(x));
            linkTopicInfo.Responses[^1].RemoveNote(x => LinkOptionsRegex().IsMatch(x));

            linkTopicInfo.Links.AddRange(destination.TopicInfo.Links);
        }
    }

    private static void ProcessKeywordLinks(Conversation conversation) {
        var keywordDestination =
            GetKeywordTopicInfoDictionary(conversation,
                info => GetKeyword(info.Responses[0].StartNotes, SimpleKeywordRegex()));
        var keywordLinks =
            GetAllKeywordTopicInfo(conversation,
                info => GetKeyword(info.Responses[^1].EndsNotes, LinkSimpleRegex()));

        foreach (var (keyword, _, linkTopicInfo) in keywordLinks) {
            if (linkTopicInfo.Links.Count > 0) {
                Console.WriteLine($"Keyword {keyword} already has links in dialogue {linkTopicInfo.Prompt}");
                continue;
            }

            if (!keywordDestination.TryGetValue(keyword, out var destination)) {
                Console.WriteLine($"Keyword {keyword} does not have a destination in any dialogue");
                continue;
            }

            destination.TopicInfo.Responses[0].RemoveNote(x => SimpleKeywordRegex().IsMatch(x));
            linkTopicInfo.Responses[^1].RemoveNote(x => LinkSimpleRegex().IsMatch(x));

            linkTopicInfo.Links.Add(destination.Topic);
            linkTopicInfo.InvisibleContinue = true;
        }
    }

    private static Dictionary<string, KeywordLink> GetKeywordTopicInfoDictionary(
        Conversation conversation,
        Func<DialogueTopicInfo, string?> getKeyword) {
        var keywordDictionary = new Dictionary<string, KeywordLink>();

        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics.SelectMany(x => x.EnumerateLinks(true))) {
                foreach (var info in topic.TopicInfos) {
                    if (info.Responses.Count == 0) continue;

                    var keyword = getKeyword(info);
                    if (keyword is null) continue;

                    if (!keywordDictionary.TryAdd(keyword, (keyword, topic, info))) {
                        Console.WriteLine(
                            $"Destination keyword {keyword} already exists in dialogue {topic.TopicInfos[0].Prompt}");
                    }
                }
            }
        }

        return keywordDictionary;
    }

    private static List<KeywordLink> GetAllKeywordTopicInfo(
        Conversation conversation,
        Func<DialogueTopicInfo, string?> getKeyword) {
        var list = new List<KeywordLink>();

        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics.SelectMany(x => x.EnumerateLinks(true))) {
                foreach (var info in topic.TopicInfos) {
                    if (info.Responses.Count == 0) continue;

                    var keyword = getKeyword(info);
                    if (keyword is null) continue;

                    list.Add((keyword, topic, info));
                }
            }
        }

        return list;
    }

    private static string? GetKeyword(List<Note> notes, Regex regex) {
        return notes
            .Select(note => regex.Match(note.Text))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .FirstOrDefault();
    }
}
