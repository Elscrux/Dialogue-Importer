using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class SharedInfoConverter : IConversationProcessor {
    public void Process(IList<GeneratedDialogue> dialogues) {
        // Convert to shared line objects that store the speaker and text per line/response
        // and links to the shared line to be able to check which lines are reused multiple times
        var sharedLines = new HashSet<SharedLine>();
        foreach (var generated in dialogues) {
            foreach (var topic in generated.Topics.EnumerateLinks()) {
                foreach (var topicInfo in topic.TopicInfos) {
                    SharedLine? last = null;
                    SharedLineLink? lastLink = null;
                    SharedLine? next = null;
                    foreach (var response in topicInfo.Responses) {
                        //Get unique shared line
                        var sharedLine = new SharedLine(response, topicInfo.Speaker);
                        if (sharedLines.TryGetValue(sharedLine, out var existingSharedLine)) {
                            sharedLine = existingSharedLine;
                        }

                        //Setup links
                        if (lastLink is not null) lastLink.Next = sharedLine;
                        lastLink = new SharedLineLink(topicInfo, last, next);
                        sharedLine.Users.Add(lastLink);
                        last = sharedLine;

                        sharedLines.Add(sharedLine);
                    }
                }
            }
        }

        // Remove lines that aren't shared, meaning they are only used once 
        sharedLines.RemoveWhere(l => l.Users.Count < 2);

        // Build a dictionary of all shared lines and potentially their common last or next line
        // depending on where the shared line is used and if they also have a common speaker
        var commonSharedLines = new List<CommonSharedLine>();
        foreach (var currentSharedLine in sharedLines) {
            var sharingLast = currentSharedLine.Users
                .Select(l => l.Last)
                .All(l => l is not null && l.Speaker == currentSharedLine.Speaker);

            var sharingNext = currentSharedLine.Users
                .Select(l => l.Next)
                .All(l => l is not null && l.Speaker == currentSharedLine.Speaker);

            commonSharedLines.Add(new CommonSharedLine(currentSharedLine) {
                CommonLast = sharingLast ? currentSharedLine.Users[0].Last : null,
                CommonNext = sharingNext ? currentSharedLine.Users[0].Next : null,
            });
        }

        // Merge shared lines that are always in the same order
        // Filter out lines that are linked to or from multiple shared lines, they can't be merged
        foreach (var current in commonSharedLines) {
            if (current.SharedLines.Count == 0) continue;

            // Try to merge the last line into the current one
            if (current.CommonLast is not null) {
                var lastLine = commonSharedLines.Find(l => l.SharedLines.Contains(current.CommonLast));
                if (lastLine is { CommonNext: not null } && lastLine.CommonNext.Equals(current.SharedLines[0])) {
                    // Add last line to current
                    current.SharedLines.InsertRange(0, lastLine.SharedLines);
                    current.CommonLast = lastLine.CommonLast;
                    lastLine.SharedLines.Clear();
                }
            }

            // Try to merge the next line into the current one
            if (current.CommonNext is not null) {
                var nextLine = commonSharedLines.Find(l => l.SharedLines.Contains(current.CommonNext));
                if (nextLine is { CommonLast: not null } && nextLine.CommonLast.Equals(current.SharedLines[^1])) {
                    // Add next line to current
                    current.SharedLines.AddRange(nextLine.SharedLines);
                    current.CommonNext = nextLine.CommonNext;
                    nextLine.SharedLines.Clear();
                }
            }
        }

        //Remove empty common shared lines
        commonSharedLines.RemoveWhere(l => l.SharedLines.Count == 0);

        const string invisibleCont = "(invis cont)";
        foreach (var commonSharedLine in commonSharedLines) {
            var firstShared = commonSharedLine.SharedLines[0];

            //Convert common shared lines to shared infos
            var sharedTopicInfo = new DialogueTopicInfo {
                Responses = [..commonSharedLine.SharedLines],
                Speaker = firstShared.Speaker,
            };

            var sharedInfo = new SharedInfo(sharedTopicInfo);

            //Integrate into dialogue structure and setup all the linking correctly
            foreach (var (topicUsingLine, _, _) in firstShared.Users) {
                var currentTopicInfo = topicUsingLine;

                //Search for topics that were nested behind invisible continues through shared dialogue
                var indexOf = currentTopicInfo.SharedInfo is null ?
                    currentTopicInfo.Responses.IndexOf(firstShared) :
                    -1;
                while (indexOf == -1
                       && currentTopicInfo is { Links: [{ TopicInfos: [{ Prompt: invisibleCont } nextTopicInfo] }] }) {
                    currentTopicInfo = nextTopicInfo;
                    if (currentTopicInfo.SharedInfo is null) indexOf = currentTopicInfo.Responses.IndexOf(firstShared);
                }

                switch (indexOf) {
                    case -1:
                        Console.Write(
                            $"ERROR: Response {firstShared.Response} is not part of {string.Join(" ", currentTopicInfo.Responses)}");
                        break;
                    case 0: {
                        // Shared info starts the topic, make the current topic the shared info
                        currentTopicInfo.SharedInfo = sharedInfo;

                        var nextRange = currentTopicInfo.Responses.GetRange(sharedTopicInfo.Responses.Count,
                            currentTopicInfo.Responses.Count - sharedTopicInfo.Responses.Count - indexOf);
                        if (nextRange.Count > 0) {
                            // If something comes after the shared info, create a new topic for it
                            // currentTopic => nextTopic
                            var nextTopic = new DialogueTopic {
                                TopicInfos = {
                                    new DialogueTopicInfo {
                                        Prompt = invisibleCont,
                                        Speaker = currentTopicInfo.Speaker,
                                        Responses = nextRange,
                                    },
                                },
                            };

                            currentTopicInfo.Append(nextTopic);
                        }

                        // Get rid of all lines that aren't part of the invisible continue
                        currentTopicInfo.Responses.RemoveRange(indexOf + sharedTopicInfo.Responses.Count,
                            currentTopicInfo.Responses.Count - sharedTopicInfo.Responses.Count);
                        break;
                    }
                    default: {
                        // Shared info is in the middle of the topic, either at the end or the middle
                        var dialogueTopicInfo = new DialogueTopicInfo {
                            Prompt = invisibleCont,
                            SharedInfo = sharedInfo,
                            Speaker = currentTopicInfo.Speaker,
                            Responses = sharedTopicInfo.Responses,
                        };
                        var invisibleContTopic = new DialogueTopic {
                            TopicInfos = [
                                dialogueTopicInfo,
                            ],
                        };
                        currentTopicInfo.Append(invisibleContTopic);

                        var nextRange = currentTopicInfo.Responses.GetRange(indexOf + sharedTopicInfo.Responses.Count,
                            currentTopicInfo.Responses.Count - sharedTopicInfo.Responses.Count - indexOf);
                        if (nextRange.Count > 0) {
                            // Inserting the shared info in the middle of other responses
                            // currentTopic => invisibleContTopic => nextTopic

                            // Build next topic from the remaining responses
                            var nextTopic = new DialogueTopic {
                                TopicInfos = [
                                    new DialogueTopicInfo {
                                        Prompt = invisibleCont,
                                        Speaker = currentTopicInfo.Speaker,
                                        Responses = nextRange,
                                    },
                                ],
                            };

                            // Handle all the linking, flags etc.
                            dialogueTopicInfo.Append(nextTopic);
                        }

                        // Get rid of all lines that aren't part of the base topic and are now part of the invisible continue or the next topic after that
                        currentTopicInfo.Responses.RemoveRange(indexOf, currentTopicInfo.Responses.Count - indexOf);
                        break;
                    }
                }
            }
        }
    }

    private sealed record SharedLineLink(DialogueTopicInfo TopicInfoUsingLine, SharedLine? Last, SharedLine? Next) {
        public SharedLine? Next { get; set; } = Next;
    }

    private sealed record SharedLine : DialogueResponse {
        public SharedLine(DialogueResponse dialogueResponse, Speaker.ISpeaker speaker) {
            Response = dialogueResponse.Response;
            ScriptNote = dialogueResponse.ScriptNote;
            Speaker = speaker;
        }

        public Speaker.ISpeaker Speaker { get; }
        public List<SharedLineLink> Users { get; } = [];

        public bool Equals(SharedLine? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) && Speaker.FormKey.Equals(other.Speaker.FormKey);
        }

        public override int GetHashCode() {
            return HashCode.Combine(base.GetHashCode(), Speaker.FormKey);
        }
    }

    private sealed class CommonSharedLine(SharedLine sharedLine) {
        public List<SharedLine> SharedLines { get; } = [sharedLine];
        public SharedLine? CommonLast { get; set; }
        public SharedLine? CommonNext { get; set; }
    }
}
