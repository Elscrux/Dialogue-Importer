using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class KeywordLinker : IConversationProcessor {
    private const string FillerRegexPart = @"[^\]]*";
    private const string MergeRegexPart = "(?:merge|go|back) ";
    private const string OptionsAfterRegexPart = "(?:options after )";

    // [DONE], [HERE]
    [GeneratedRegex($"^{KeywordUtils.KeywordRegexPart}$")]
    private static partial Regex SimpleKeywordRegex();

    // [merge to DONE above]
    [GeneratedRegex($"(?i){FillerRegexPart}(?:{MergeRegexPart})?to (?-i){KeywordUtils.KeywordRegexPart}{FillerRegexPart}")]
    private static partial Regex LinkSimpleRegex();

    // [merge to options after HERE above]
    [GeneratedRegex($"(?i){FillerRegexPart}(?:{MergeRegexPart})?to "
      + $"{OptionsAfterRegexPart}?(?-i){KeywordUtils.KeywordRegexPart}{FillerRegexPart}")]
    private static partial Regex LinkOptionsRegex();

    public void Process(Conversation conversation) {
        ProcessKeywordLinks(conversation);
        ProcessOptionLinks(conversation);
    }

    private static void ProcessOptionLinks(Conversation conversation) {
        var optionsDestinations = conversation.GetKeywordTopicInfoDictionary(
            SimpleKeywordRegex(),
            info => info.Responses.Count == 0 ? [] : info.Responses[^1].EndNotesAndStartIfResponseEmpty());
        var optionsLinks = conversation.GetAllKeywordTopicInfos(
            LinkOptionsRegex(),
            info => info.Responses.Count == 0 ? [] : info.Responses[^1].EndNotesAndStartIfResponseEmpty());

        foreach (var (note, keyword, _, linkTopicInfo) in optionsLinks) {
            if (linkTopicInfo.Links.Count > 0) {
                Console.WriteLine($"Keyword {keyword} already has links in dialogue {linkTopicInfo.Prompt.FullText}");
                continue;
            }

            if (!optionsDestinations.TryGetValue(keyword, out var destination)) {
                Console.WriteLine($"Keyword {keyword} does not have a destination in any dialogue");
                continue;
            }

            destination.TopicInfo.Responses[^1].RemoveNote(x => x == keyword);
            destination.TopicInfo.RemoveRedundantResponses();
            linkTopicInfo.Responses[^1].RemoveNote(note);
            linkTopicInfo.RemoveRedundantResponses();

            linkTopicInfo.Links.AddRange(destination.TopicInfo.Links);
        }
    }

    private static void ProcessKeywordLinks(Conversation conversation) {
        var keywordDestination = conversation.GetKeywordTopicInfoDictionary(
            SimpleKeywordRegex(),
            info => info.Responses.Count == 0 ? [] : info.Responses.SelectMany(x => x.StartNotes));
        var promptKeywordLinks = conversation.GetAllKeywordTopicInfos(
            LinkSimpleRegex(),
            info => info.Prompt.EndNotesAndStartIfResponseEmpty());
        var responseKeywordLinks = conversation.GetAllKeywordTopicInfos(
            LinkSimpleRegex(),
            info => info.Responses.Count == 0 ? [] : info.Responses[^1].EndNotesAndStartIfResponseEmpty());

        var removeNotes = new List<Action>();

        foreach (var (note, keyword, _, linkTopicInfo) in promptKeywordLinks) {
            PerformLinking(linkTopicInfo,
                keyword,
                topicInfo => topicInfo.Prompt.RemoveNote(note));
        }

        foreach (var (note, keyword, _, linkTopicInfo) in responseKeywordLinks) {
            PerformLinking(linkTopicInfo,
                keyword,
                topicInfo => {
                    topicInfo.Responses[^1].RemoveNote(note);
                    topicInfo.RemoveRedundantResponses();
                });
        }

        foreach (var removeNote in removeNotes) {
            removeNote();
        }

        void PerformLinking(DialogueTopicInfo linkTopicInfo, string keyword, Action<DialogueTopicInfo> removeNote) {
            if (linkTopicInfo.Links.Count > 0) {
                Console.WriteLine($"Keyword {keyword} already has links in dialogue {linkTopicInfo.Prompt.FullText}");
                return;
            }

            if (!keywordDestination.TryGetValue(keyword, out var destination)) {
                Console.WriteLine($"Keyword {keyword} does not have a destination in any dialogue");
                return;
            }

            var response = destination.TopicInfo.Responses.FirstOrDefault(r => r.HasNote(x => x == keyword));
            while (response is null && destination.TopicInfo is {
                InvisibleContinue: true,
                Links: [{ TopicInfos: [{} invisibleContinue] }]
            }) {
                response = invisibleContinue.Responses.FirstOrDefault(r => r.HasNote(x => x == keyword));
            }

            if (response is null) {
                throw new InvalidOperationException(
                    $"Keyword {keyword} does not have a response in dialogue {destination.TopicInfo.Prompt.FullText}");
            }

            var responseIndex = destination.TopicInfo.Responses.IndexOf(response);

            if (responseIndex == 0) {
                // Can link to topic info directly
                linkTopicInfo.Links.Add(destination.Topic);
                linkTopicInfo.InvisibleContinue = true;

                removeNotes.Add(() => {
                    destination.TopicInfo.RemoveNote(destination.Note);
                    destination.TopicInfo.RemoveRedundantResponses();
                    removeNote(linkTopicInfo);
                });
            } else {
                // Need to split topic info to add link
                var responsesStartingAtIndex = destination.TopicInfo.Responses
                    .Skip(responseIndex)
                    .ToList();
                var splitOffTopicInfo = new DialogueTopicInfo {
                    Speaker = linkTopicInfo.Speaker,
                    Responses = responsesStartingAtIndex,
                };
                var (topic, topicInfo) = destination.TopicInfo.SplitOffDialogue(splitOffTopicInfo);
                if (topic is null) return;

                linkTopicInfo.Links.Add(topic);
                linkTopicInfo.InvisibleContinue = true;

                removeNotes.Add(() => {
                    topicInfo.RemoveNote(destination.Note);
                    topicInfo.RemoveRedundantResponses();
                    removeNote(linkTopicInfo);
                });
            }
        }
    }
}
