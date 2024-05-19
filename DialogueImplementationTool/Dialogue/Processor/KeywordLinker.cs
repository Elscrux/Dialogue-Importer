using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

using KeywordLink = (string Keyword, DialogueTopic Topic, DialogueTopicInfo TopicInfo);

public sealed partial class KeywordLinker : IConversationProcessor {
    private const string FillerRegexPart = @"[^\]]*";
    private const string MergeRegexPart = "(?:merge|go|back)";
    private const string OptionsAfterRegexPart = "(?:options after )";
    private const string KeywordRegexPart = "([A-Z]+)";

    // [DONE] We should bring this back to Telwyne. She'll probably have some ideas on where these eels might be burrowed.
    [GeneratedRegex($@"^\[{KeywordRegexPart}\]")]
    private static partial Regex KeywordDestinationRegex();

    // I guess the effects of the bait must have thrown me off. [merge to DONE above]
    [GeneratedRegex($@"\[{FillerRegexPart}{MergeRegexPart} to {KeywordRegexPart}{FillerRegexPart}\]$")]
    private static partial Regex KeywordLinkRegex();

    // Anyway, I need you to go fetch him before they try to drown him in the waves. Think you can manage that? [HERE]
    [GeneratedRegex($@"\[{KeywordRegexPart}\]$")]
    private static partial Regex OptionsDestinationRegex();

    // Decided you weren't so busy after all? [merge to options after HERE above]
    [GeneratedRegex($@"\[{FillerRegexPart}{MergeRegexPart} to {OptionsAfterRegexPart}?{KeywordRegexPart}{FillerRegexPart}\]$")]
    private static partial Regex OptionsLinkRegex();

    public void Process(IList<GeneratedDialogue> dialogues) {
        ProcessKeywordLinks(dialogues);
        ProcessOptionLinks(dialogues);
    }

    private static void ProcessOptionLinks(IList<GeneratedDialogue> dialogues) {
        var optionsDestinations =
            GetKeywordTopicInfoDictionary(dialogues, info => OptionsDestinationRegex().Match(info.Responses[^1].Response));
        var optionsLinks =
            GetAllKeywordTopicInfo(dialogues, info => OptionsLinkRegex().Match(info.Responses[^1].Response));

        foreach (var (keyword, _, linkTopicInfo) in optionsLinks) {
            if (linkTopicInfo.Links.Count > 0) {
                Console.WriteLine($"Keyword {keyword} already has links in dialogue {linkTopicInfo.Prompt}");
                continue;
            }

            if (!optionsDestinations.TryGetValue(keyword, out var destination)) {
                Console.WriteLine($"Keyword {keyword} does not have a destination in any dialogue");
                continue;
            }

            destination.TopicInfo.Responses[^1].Response = OptionsDestinationRegex()
                .Replace(destination.TopicInfo.Responses[^1].Response, string.Empty)
                .Trim();

            linkTopicInfo.Responses[^1].Response = OptionsLinkRegex()
                .Replace(linkTopicInfo.Responses[^1].Response, string.Empty)
                .Trim();

            linkTopicInfo.Links.AddRange(destination.TopicInfo.Links);
        }
    }

    private static void ProcessKeywordLinks(IList<GeneratedDialogue> dialogues) {
        var keywordDestination =
            GetKeywordTopicInfoDictionary(dialogues, info => KeywordDestinationRegex().Match(info.Responses[0].Response));
        var keywordLinks =
            GetAllKeywordTopicInfo(dialogues, info => KeywordLinkRegex().Match(info.Responses[^1].Response));

        foreach (var (keyword, _, linkTopicInfo) in keywordLinks) {
            if (linkTopicInfo.Links.Count > 0) {
                Console.WriteLine($"Keyword {keyword} already has links in dialogue {linkTopicInfo.Prompt}");
                continue;
            }

            if (!keywordDestination.TryGetValue(keyword, out var destination)) {
                Console.WriteLine($"Keyword {keyword} does not have a destination in any dialogue");
                continue;
            }

            destination.TopicInfo.Responses[0].Response = KeywordDestinationRegex()
                .Replace(destination.TopicInfo.Responses[0].Response, string.Empty)
                .Trim();

            linkTopicInfo.Responses[^1].Response = KeywordLinkRegex()
                .Replace(linkTopicInfo.Responses[^1].Response, string.Empty)
                .Trim();

            linkTopicInfo.Links.Add(destination.Topic);
            linkTopicInfo.InvisibleContinue = true;
        }
    }

    private static Dictionary<string, KeywordLink> GetKeywordTopicInfoDictionary(
        IList<GeneratedDialogue> dialogues,
        Func<DialogueTopicInfo, Match> regex) {
        var keywordDictionary = new Dictionary<string, KeywordLink>();

        foreach (var dialogue in dialogues) {
            foreach (var topic in dialogue.Topics.SelectMany(x => x.EnumerateLinks())) {
                foreach (var info in topic.TopicInfos) {
                    if (info.Responses.Count == 0) continue;

                    var match = regex(info);
                    if (!match.Success) continue;

                    var keyword = match.Groups[1].Value;
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
        IList<GeneratedDialogue> dialogues,
        Func<DialogueTopicInfo, Match> regex) {
        var list = new List<KeywordLink>();

        foreach (var dialogue in dialogues) {
            foreach (var topic in dialogue.Topics.SelectMany(x => x.EnumerateLinks())) {
                foreach (var info in topic.TopicInfos) {
                    if (info.Responses.Count == 0) continue;

                    var match = regex(info);
                    if (!match.Success) continue;

                    var keyword = match.Groups[1].Value;
                    list.Add((keyword, topic, info));
                }
            }
        }

        return list;
    }
}
