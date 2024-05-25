using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

/// <summary>
/// Merges responses that have no text and link to something as invisible continue.
/// <example>
/// <para>The invisible continue is replaced by a shared info</para>
/// <para>Before:</para>
/// <code>
/// <para>1. Hi, Player. How are you?</para>
/// <para>	1.1. [Empty, Invisible Continue, Link to 2.1]</para>
/// <para>2. How are you Player?</para>
/// <para>	2.1. I'm good. [back to options]</para>
/// </code>
/// <para>After:</para>
/// <code>
/// <para>1. Hi, Player. How are you?</para>
/// <para>	2.1. [Shared] I'm good. [back to options]</para>
/// <para>2. How are you Player?</para>
/// <para>	2.1. [Shared] I'm good. [back to options]</para>
/// </code>
/// </example>
/// </summary>
public sealed class CollapseEmptyInvisibleContinues : IConversationProcessor {
    public void Process(Conversation conversation) {
        foreach (var dialogue in conversation) {
            foreach (var topic in dialogue.Topics.EnumerateLinks(true)) {
                foreach (var topicInfo in topic.TopicInfos) {
                    Process(topicInfo);
                }
            }
        }
    }

    private static void Process(DialogueTopicInfo topicInfo) {
        if (topicInfo.SharedInfo is not null) return;
        if (topicInfo.Responses.Count > 0 && !topicInfo.Responses[0].IsEmpty()) return;
        if (topicInfo.Links is not [{ TopicInfos: [var nextTopicInfo] }]) return;

        var sharedInfo = nextTopicInfo.MakeSharedInfo();
        topicInfo.ApplySharedInfo(sharedInfo);
        topicInfo.Links.Clear();
        topicInfo.Links.AddRange(nextTopicInfo.Links);
    }
}
