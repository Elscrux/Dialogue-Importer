using System;
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
        // todo support links to the middle of dialogue (create shared infos and so on)
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
            info => info.Responses.Count == 0 ? [] : info.Responses[0].StartNotes);
        var promptKeywordLinks = conversation.GetAllKeywordTopicInfos(
            LinkSimpleRegex(),
            info => info.Prompt.EndNotesAndStartIfResponseEmpty());
        var responseKeywordLinks = conversation.GetAllKeywordTopicInfos(
            LinkSimpleRegex(),
            info => info.Responses.Count == 0 ? [] : info.Responses[^1].EndNotesAndStartIfResponseEmpty());

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

        void PerformLinking(DialogueTopicInfo linkTopicInfo, string keyword, Action<DialogueTopicInfo> removeNote) {
            if (linkTopicInfo.Links.Count > 0) {
                Console.WriteLine($"Keyword {keyword} already has links in dialogue {linkTopicInfo.Prompt.FullText}");
                return;
            }

            if (!keywordDestination.TryGetValue(keyword, out var destination)) {
                Console.WriteLine($"Keyword {keyword} does not have a destination in any dialogue");
                return;
            }

            destination.TopicInfo.Responses[0].RemoveNote(x => x == keyword);
            destination.TopicInfo.RemoveRedundantResponses();
            removeNote(linkTopicInfo);

            linkTopicInfo.Links.Add(destination.Topic);
            linkTopicInfo.InvisibleContinue = true;
        }
    }
}
