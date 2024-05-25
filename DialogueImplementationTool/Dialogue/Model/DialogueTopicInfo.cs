using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DynamicData;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Model;

[DebuggerDisplay("{ToString()}")]
public sealed class DialogueTopicInfo {
    public SharedInfo? SharedInfo { get; set; }

    public Speaker.ISpeaker Speaker { get; set; } = null!;

    public string Prompt { get; set; } = string.Empty;
    public List<DialogueResponse> Responses { get; init; } = [];
    public List<DialogueTopic> Links { get; init; } = [];
    public bool SayOnce { get; set; }
    public bool Goodbye { get; set; }
    public bool InvisibleContinue { get; set; }
    public bool Random { get; set; }
    public float ResetHours { get; set; }
    public List<Condition> ExtraConditions { get; init; } = [];

    public IEnumerable<Note> AllNotes() => Responses.SelectMany(r => r.Notes());

    public DialogueTopicInfo CopyWith(IEnumerable<DialogueResponse> newResponses) {
        return new DialogueTopicInfo {
            SharedInfo = SharedInfo,
            Speaker = Speaker,
            Prompt = Prompt,
            Responses = newResponses.ToList(),
            Links = Links.ToList(),
            SayOnce = SayOnce,
            Goodbye = Goodbye,
            InvisibleContinue = InvisibleContinue,
            Random = Random,
        };
    }

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

    public DialogueTopicInfo SplitOffDialogue(DialogueTopicInfo splitOffTopicInfo) {
        var startingResponse = splitOffTopicInfo.Responses[0];

        //Search for topics that were nested behind invisible continues through shared dialogue
        var currentInfo = this;
        var indexOf = currentInfo.SharedInfo is null
            ? currentInfo.Responses.IndexOf(startingResponse)
            : -1;

        while (indexOf == -1
               && currentInfo is { InvisibleContinue: true, Links: [{ TopicInfos: [var nextTopicInfo] }] }) {
            currentInfo = nextTopicInfo;
            if (currentInfo.SharedInfo is null) indexOf = currentInfo.Responses.IndexOf(startingResponse);
        }

        switch (indexOf) {
            case -1:
                throw new InvalidOperationException(
                    $"ERROR: Response {startingResponse.FullResponse} is not part of {string.Join(" ", currentInfo.Responses)}");
            case 0: {
                // Split info starts the topic, make the current topic the split info
                var nextRange = currentInfo.Responses.GetRange(
                    splitOffTopicInfo.Responses.Count,
                    currentInfo.Responses.Count - splitOffTopicInfo.Responses.Count);
                if (nextRange.Count > 0) {
                    // If something comes after the split info, create a new topic for it
                    // currentTopic => nextTopic
                    var nextTopic = new DialogueTopic {
                        TopicInfos = {
                            new DialogueTopicInfo {
                                Speaker = currentInfo.Speaker,
                                Responses = nextRange,
                            },
                        },
                    };

                    currentInfo.Append(nextTopic);
                }

                // Get rid of all lines that aren't part of the invisible continue
                currentInfo.Responses.RemoveRange(
                    splitOffTopicInfo.Responses.Count,
                    currentInfo.Responses.Count - splitOffTopicInfo.Responses.Count);
                return currentInfo;
            }
            default: {
                // Split info is in the middle of the topic, either at the end or the middle
                var invisibleContTopicInfo = new DialogueTopicInfo {
                    Speaker = currentInfo.Speaker,
                    Responses = splitOffTopicInfo.Responses,
                };
                var invisibleContTopic = new DialogueTopic { TopicInfos = [invisibleContTopicInfo] };
                currentInfo.Append(invisibleContTopic);

                var nextRange = currentInfo.Responses.GetRange(
                    indexOf + splitOffTopicInfo.Responses.Count,
                    currentInfo.Responses.Count - splitOffTopicInfo.Responses.Count - indexOf);
                if (nextRange.Count > 0) {
                    // Inserting the split info in the middle of other responses
                    // currentTopic => invisibleContTopic => nextTopic

                    // Build next topic from the remaining responses
                    var nextTopic = new DialogueTopic {
                        TopicInfos = [
                            new DialogueTopicInfo {
                                Speaker = currentInfo.Speaker,
                                Responses = nextRange,
                            },
                        ],
                    };

                    // Handle all the linking, flags etc.
                    invisibleContTopicInfo.Append(nextTopic);
                }

                // Get rid of all lines that aren't part of the base topic and are now part of the invisible continue or the next topic after that
                currentInfo.Responses.RemoveRange(indexOf, currentInfo.Responses.Count - indexOf);
                return invisibleContTopicInfo;
            }
        }
    }

    public void RemoveNote(Note note) {
        foreach (var response in Responses) {
            response.RemoveNote(note);
        }
    }

    public override string ToString() {
        if (SharedInfo is not null) return $"[Shared] {GetResponseString(SharedInfo.ResponseDataTopicInfo)}";

        return GetResponseString(this);

        string GetResponseString(DialogueTopicInfo topicInfo) {
            if (topicInfo.Responses.Count == 0) return "Empty topic info";

            return string.Join(" | ", topicInfo.Responses);
        }
    }

    public void RemoveRedundantResponses() {
        Responses.RemoveAll(r => r.IsEmpty() && r.Notes().Count == 0);
    }
}
