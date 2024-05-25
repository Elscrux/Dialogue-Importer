using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

/// <summary>
/// Merges responses that have no text and just notes into the previous response.
/// <example>
/// <para>Here we merge the [back to options] note in 1.2 into 1.1.</para>
/// <para>Before:</para>
/// <code>
/// <para>1. Hi, Player. How are you?</para>
/// <para>	1.1. I'm good.</para>
/// <para>	1.2. [back to options]</para>
/// </code>
/// <para>After:</para>
/// <code>
/// <para>1. Hi, Player. How are you?</para>
/// <para>	1.1. I'm good. [back to options]</para>
/// </code>
/// </example>
/// </summary>
public sealed class CollapseNoteOnlyResponse : IConversationProcessor {
    public void Process(Conversation conversation) {
        foreach (var generatedDialogue in conversation) {
            foreach (var topic in generatedDialogue.Topics.EnumerateLinks(true)) {
                foreach (var topicInfo in topic.TopicInfos) {
                    Process(topicInfo);
                }
            }
        }
    }

    public void Process(DialogueTopicInfo topicInfo) {
        // Merge start notes into next line if applicable
        while (topicInfo.Responses.Count > 1) {
            var firstResponse = topicInfo.Responses[0];
            var secondResponse = topicInfo.Responses[1];
            if (firstResponse.IsEmpty() && firstResponse.Notes().Count > 0) {
                secondResponse.StartNotes.AddRange(firstResponse.Notes());
                topicInfo.Responses.RemoveAt(0);
            } else {
                break;
            }
        }

        // Merge notes into previous line if applicable
        var counter = 1;
        while (counter < topicInfo.Responses.Count) {
            var response = topicInfo.Responses[counter];

            if (response.IsEmpty() && response.Notes().Count > 0) {
                topicInfo.Responses[counter - 1].EndsNotes.AddRange(response.Notes());
                topicInfo.Responses.RemoveAt(counter);
            } else {
                counter++;
            }
        }
    }
}
