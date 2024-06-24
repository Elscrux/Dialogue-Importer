using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class SharedInfoConverter : IConversationProcessor {
    public void Process(Conversation conversation) {
        // Convert to shared line objects that store the speaker and text per line/response
        // and links to the shared line to be able to check which lines are reused multiple times
        var sharedLines = new HashSet<SharedLine>();
        var topics = conversation
            .SelectMany(x => x.Topics.EnumerateLinks(true))
            .Distinct();

        foreach (var topic in topics) {
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

        // Remove lines that aren't shared, meaning they are only used once 
        sharedLines.RemoveWhere(l => l.Users.Count < 2);

        // Build a dictionary of all shared lines and potentially their common last or next line
        // depending on where the shared line is used and if they also have a common speaker
        var commonSharedLines = new List<CommonSharedLine>();
        foreach (var currentSharedLine in sharedLines) {
            var sharingLast = currentSharedLine.Users
                .Select(l => l.Last)
                .All(l => l is not null && l.Equals(currentSharedLine.Users[0].Last) && l.Speaker.Equals(currentSharedLine.Speaker));

            var sharingNext = currentSharedLine.Users
                .Select(l => l.Next)
                .All(n => n is not null && n.Equals(currentSharedLine.Users[0].Next) && n.Speaker.Equals(currentSharedLine.Speaker));

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

        foreach (var commonSharedLine in commonSharedLines) {
            var firstShared = commonSharedLine.SharedLines[0];

            //Convert common shared lines to shared infos
            var sharedTopicInfo = new DialogueTopicInfo {
                Responses = [..commonSharedLine.SharedLines],
                Speaker = firstShared.Speaker,
            };

            // when all topic infos that use a shared line link to the same next line(s) (or none at all), merge those again or make sure it never splits
            // exclude lines that don't have a previous line, because in this case they wouldn't be able to be linked to without an empty line as proxy in between
            if (commonSharedLine.SharedLines.TrueForAll(x => x.Users.TrueForAll(link => link.Last is not null))
             && firstShared.Users
                    .Skip(1)
                    .All(x => x.TopicInfoUsingLine.Links.SequenceEqual(firstShared.Users[0].TopicInfoUsingLine.Links))) {
                // Split off for all users
                foreach (var (topicUsingLine, _, _) in firstShared.Users) {
                    var matchingTopicInfo = topicUsingLine
                        .EnumerateInvisibleContinues()
                        .FirstOrDefault(info => info.Responses.Contains(sharedTopicInfo.Responses[0]));
                    if (matchingTopicInfo is null) continue;

                    topicUsingLine.SplitOffDialogue(sharedTopicInfo);
                }

                // Then make sure all link to the same split off object
                var links = firstShared.Users[0].TopicInfoUsingLine.Links.ToList();
                foreach (var (topicUsingLine, _, _) in firstShared.Users.Skip(1)) {
                    topicUsingLine.Links.Clear();
                    topicUsingLine.Links.AddRange(links);
                }
            } else {
                // otherwise, create shared info
                var sharedInfo = new SharedInfo(sharedTopicInfo);

                //Integrate into dialogue structure and setup all the linking correctly
                foreach (var (topicUsingLine, _, _) in firstShared.Users) {
                    var matchingTopicInfo = topicUsingLine
                        .EnumerateInvisibleContinues()
                        .FirstOrDefault(info => info.Responses.Contains(sharedTopicInfo.Responses[0]));
                    if (matchingTopicInfo is null) continue;

                    if (topicUsingLine.SharedInfo is not null) continue;

                    var dialogueTopicInfo = topicUsingLine.SplitOffDialogue(sharedTopicInfo);
                    dialogueTopicInfo.ApplySharedInfo(sharedInfo);
                }
            }
        }
    }

    private sealed record SharedLineLink(DialogueTopicInfo TopicInfoUsingLine, SharedLine? Last, SharedLine? Next) {
        public SharedLine? Next { get; set; } = Next;
    }

    private sealed class SharedLine : DialogueResponse, IEquatable<SharedLine> {
        public SharedLine(DialogueResponse dialogueResponse, ISpeaker speaker) {
            Response = dialogueResponse.Response;
            StartNotes = dialogueResponse.StartNotes;
            EndsNotes = dialogueResponse.EndsNotes;
            ScriptNote = dialogueResponse.ScriptNote;
            Speaker = speaker;
        }

        public ISpeaker Speaker { get; }
        public List<SharedLineLink> Users { get; } = [];

        public override bool Equals(object? obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return Equals(obj as SharedLine);
        }

        public bool Equals(SharedLine? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(this, other)
             && Speaker.FormKey.Equals(other.Speaker.FormKey);
        }

        public override int GetHashCode() {
            return HashCode.Combine(GetHashCode(this), Speaker.FormKey);
        }
    }

    private sealed class CommonSharedLine(SharedLine sharedLine) {
        public List<SharedLine> SharedLines { get; } = [sharedLine];
        public SharedLine? CommonLast { get; set; }
        public SharedLine? CommonNext { get; set; }
    }
}
