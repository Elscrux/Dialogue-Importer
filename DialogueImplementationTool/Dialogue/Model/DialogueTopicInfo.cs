using System.Collections.Generic;
using DynamicData;
namespace DialogueImplementationTool.Dialogue.Model;

public sealed record DialogueTopicInfo {
    public SharedInfo? SharedInfo { get; set; }

    public Speaker.ISpeaker Speaker { get; set; }

    public string Prompt { get; set; } = string.Empty;
    public List<DialogueResponse> Responses { get; init; } = [];
    public List<DialogueTopic> Links { get; init; } = [];
    public bool SayOnce { get; set; }
    public bool Goodbye { get; set; }
    public bool InvisibleContinue { get; set; }
    public bool Random { get; set; }

    /// <summary>
    ///     Links dialogue to be played after this topic, linked with an invisible continue
    ///     This handles all relinking of topics, flags, etc.
    /// </summary>
    /// <param name="nextTopic">Topic to be appended</param>
    public void Append(DialogueTopic nextTopic) {
        // Handle invisible continue
        InvisibleContinue = true;

        // Handle Goodbye
        if (Goodbye) {
            foreach (var info in nextTopic.TopicInfos) {
                info.Goodbye = true;
            }

            Goodbye = false;
        }

        // Handle Links
        // Move current links to next topic
        foreach (var info in nextTopic.TopicInfos) {
            info.Links.Add(Links);
        }

        // Retarget links to next topic
        Links.Clear();
        Links.Add(nextTopic);
    }
}
