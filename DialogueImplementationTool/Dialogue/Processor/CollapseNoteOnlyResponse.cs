using DialogueImplementationTool.Dialogue.Model;
using Noggog;
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
public sealed class CollapseNoteOnlyResponse : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        var counter = 1;
        while (counter < topicInfo.Responses.Count) {
            var response = topicInfo.Responses[counter];

            if (response.Response.IsNullOrEmpty() && response.Notes().Count > 0) {
                topicInfo.Responses[counter - 1].EndsNotes.AddRange(response.Notes());
                topicInfo.Responses.RemoveAt(counter);
            } else {
                counter++;
            }
        }
    }
}
