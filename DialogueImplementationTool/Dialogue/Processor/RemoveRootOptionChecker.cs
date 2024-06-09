using System;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

/// <summary>
/// Replaces [remove root option] notes with locks on the root option.
/// <example>
/// <para>Here we merge the [back to options] note in 1.2 into 1.1.</para>
/// <para>Before:</para>
/// <code>
/// <para>1. Hi, Player. How are you?</para>
/// <para>	1.1. I'm good. [remove root option]</para>
/// </code>
/// <para>After:</para>
/// <code>
/// <para>1. [5C0ED72F_AC54_4E64_B2BA_15FA95FCF95A] Hi, Player. How are you?</para>
/// <para>	1.1. I'm good. [Lock 5C0ED72F_AC54_4E64_B2BA_15FA95FCF95A]</para>
/// </code>
/// </example>
/// </summary>
public sealed partial class RemoveRootOptionChecker : IConversationProcessor {
    // [merge to DONE above]
    [GeneratedRegex("(remove|lock) root option", RegexOptions.IgnoreCase)]
    private static partial Regex RemoveRootOption();

    // [DONE], [HERE]
    [GeneratedRegex($"^{KeywordUtils.KeywordRegexPart}$")]
    private static partial Regex OnlyKeywordRegex();

    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            foreach (var rootTopic in dialogue.Topics) {
                if (rootTopic.TopicInfos.Count == 0) continue;
                var lockedKeyword = GetLockedKeyword(rootTopic);
                
                var hasNotes = false;

                foreach (var linkedTopic in rootTopic.EnumerateLinks(true)) {
                    foreach (var topicInfo in linkedTopic.TopicInfos) {
                        if (topicInfo.Responses.Count == 0) continue;
                        if (!topicInfo.Responses[^1].RemoveNote(note => RemoveRootOption().IsMatch(note))) continue;

                        topicInfo.Responses[^1].EndsNotes.Add(new Note { Text = $"Lock {lockedKeyword}" });

                        if (hasNotes) continue;

                        hasNotes = true;
                        foreach (var rootTopicInfo in rootTopic.TopicInfos) {
                            rootTopicInfo.Prompt.StartNotes.Add(new Note { Text = lockedKeyword });
                        }
                    }
                }
            }
        }
    }

    private static string GetLockedKeyword(DialogueTopic rootTopic) {
        // Use existing keyword note if it exists
        if (rootTopic.TopicInfos is [var topicInfo]) {
            var keywordNote = topicInfo.Prompt.StartNotes.Find(note => OnlyKeywordRegex().IsMatch(note.Text));
            if (keywordNote is not null) {
                return keywordNote.Text;
            }
        }

        // Otherwise generate non-conflicting keyword
        return Guid.NewGuid()
            .ToString()
            .ToUpper()
            .Replace("-", "_");
    }
}
