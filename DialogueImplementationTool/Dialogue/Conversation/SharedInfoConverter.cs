using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Responses;
using DialogueImplementationTool.Dialogue.Topics;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Conversation;

public sealed class SharedInfoConverter : IConversationProcessor {
    private sealed record SharedLineLink(DialogueTopic TopicUsingLine, SharedLine? Last, SharedLine? Next) {
        public SharedLine? Next { get; set; } = Next;
    }
    
    private sealed record SharedLine : DialogueResponse {
        public SharedLine(DialogueResponse dialogueResponse, ISpeaker speaker) {
            Response = dialogueResponse.Response;
            ScriptNote = dialogueResponse.ScriptNote;
            Speaker = speaker;
        }
        
        public ISpeaker Speaker { get; }
        public List<SharedLineLink> Users { get; } = new();

        public bool Equals(SharedLine? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) && Speaker.FormKey.Equals(other.Speaker.FormKey);
        }
        public override int GetHashCode() {
            return HashCode.Combine(base.GetHashCode(), Speaker.FormKey);
        }
    }
    
    private sealed class CommonSharedLine {
        public CommonSharedLine(SharedLine sharedLine) {
            SharedLines.Add(sharedLine);
        }
        
        public List<SharedLine> SharedLines { get; } = new();
        
        public SharedLine? CommonLast { get; set; }
        public SharedLine? CommonNext { get; set; }
    }

    public void Process(IList<GeneratedDialogue> dialogue) {
        // Convert to shared line objects that store the speaker and text per line/response
        // and links to the shared line to be able to check which lines are reused multiple times
        var sharedLines = new HashSet<SharedLine>();
        foreach (var generated in dialogue) {
            foreach (var rootTopic in generated.Topics) {
                foreach (var topic in rootTopic.EnumerateLinks()) {
                    SharedLine? last = null;
                    SharedLineLink? lastLink = null;
                    SharedLine? next = null;
                    foreach (var response in topic.Responses) {
                        //Get unique shared line
                        var sharedLine = new SharedLine(response, topic.Speaker);
                        if (sharedLines.TryGetValue(sharedLine, out var existingSharedLine)) {
                            sharedLine = existingSharedLine;
                        }
                    
                        //Setup links
                        if (lastLink != null) lastLink.Next = sharedLine;
                        lastLink = new SharedLineLink(topic, last, next);
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
                .NotNull()
                .Where(l => l.Speaker == currentSharedLine.Speaker)
                .Distinct()
                .Count() == 1;

            var sharingNext = currentSharedLine.Users
                .Select(l => l.Next)
                .NotNull()
                .Where(l => l.Speaker == currentSharedLine.Speaker)
                .Distinct()
                .Count() == 1;

            commonSharedLines.Add(new CommonSharedLine(currentSharedLine) {
                CommonLast = sharingLast ? currentSharedLine.Users[0].Last : null,
                CommonNext = sharingNext ? currentSharedLine.Users[0].Next : null,
            });
        }
        
        //Merge shared lines that are always in the same order
        //Filter out lines that are linked to or from multiple shared lines, they can't be merged
        foreach (var current in commonSharedLines) {
            if (current.SharedLines.Count == 0) continue;

            if (current.CommonLast != null) {
                var lastLine = commonSharedLines.FirstOrDefault(l => l.SharedLines.Contains(current.CommonLast));
                if (lastLine is { CommonNext: {} } && lastLine.CommonNext.Equals(current.SharedLines[0])) {
                    //Add last line to current
                    current.SharedLines.InsertRange(0, lastLine.SharedLines);
                    current.CommonLast = lastLine.CommonLast;
                    lastLine.SharedLines.Clear();
                }
            }
            
            if (current.CommonNext != null) {
                var nextLine = commonSharedLines.FirstOrDefault(l => l.SharedLines.Contains(current.CommonNext));
                if (nextLine is { CommonLast: not null } && nextLine.CommonLast.Equals(current.SharedLines[^1])) {
                    //Add last line to current
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
            var sharedTopic = new DialogueTopic();
            sharedTopic.Responses.AddRange(commonSharedLine.SharedLines);
            sharedTopic.Speaker = firstShared.Speaker;
            
            var sharedInfo = new SharedInfo(sharedTopic);
            
            //Integrate into dialogue structure and setup all the linking correctly
            foreach (var (topicUsingLine, _, _) in firstShared.Users) {
                var currentTopic = topicUsingLine;
                
                //Search for topics that were nested behind invis conts through shared dialogue
                var indexOf = currentTopic.SharedInfo == null ? currentTopic.Responses.IndexOf(firstShared) : -1;
                while (indexOf == -1 && currentTopic.Links.Count == 1 && currentTopic.Links[0].Text == invisibleCont) {
                    currentTopic = currentTopic.Links[0];
                    if (currentTopic.SharedInfo == null) {
                        indexOf = currentTopic.Responses.IndexOf(firstShared);
                    }
                }
                
                if (indexOf == -1) {
                    Console.Write($"ERROR: Response {firstShared.Response} is not part of {string.Join(" ", currentTopic.Responses)}");
                } else if (indexOf == 0) {
                    // Shared info starts the topic, make the current topic the shared info
                    currentTopic.SharedInfo = sharedInfo;

                    var nextRange = currentTopic.Responses.GetRange(sharedTopic.Responses.Count, currentTopic.Responses.Count - sharedTopic.Responses.Count - indexOf);
                    if (nextRange.Count > 0) {
                        // If something comes after the shared info, create a new topic for it
                        // currentTopic => nextTopic
                        var nextTopic = new DialogueTopic {
                            Text = invisibleCont,
                            IncomingLink = currentTopic,
                            Speaker = currentTopic.Speaker,
                        };
                        nextTopic.Responses.AddRange(nextRange);
                        
                        currentTopic.Append(nextTopic);
                    }

                    // Get rid of all lines that aren't part of the invisible continue
                    currentTopic.Responses.RemoveRange(indexOf + sharedTopic.Responses.Count, currentTopic.Responses.Count - sharedTopic.Responses.Count);
                } else {
                    // Shared info is in the middle of the topic, either at the end or the middle
                    var invisibleContTopic = new DialogueTopic {
                        Text = invisibleCont,
                        IncomingLink = currentTopic,
                        SharedInfo = sharedInfo,
                        Speaker = currentTopic.Speaker,
                    };
                    invisibleContTopic.Responses.AddRange(sharedTopic.Responses);
                    currentTopic.Append(invisibleContTopic);

                    var nextRange = currentTopic.Responses.GetRange(indexOf + sharedTopic.Responses.Count, currentTopic.Responses.Count - sharedTopic.Responses.Count - indexOf);
                    if (nextRange.Count > 0) {
                        // Inserting the shared info in the middle of other responses
                        // currentTopic => invisibleContTopic => nextTopic
                        
                        // Build next topic from the remaining responses
                        var nextTopic = new DialogueTopic {
                            Text = invisibleCont,
                            IncomingLink = invisibleContTopic,
                            Speaker = currentTopic.Speaker,
                        };
                        nextTopic.Responses.AddRange(nextRange);
                        
                        // Handle all the linking, flags etc.
                        invisibleContTopic.Append(nextTopic);
                    }

                    // Get rid of all lines that aren't part of the base topic and are now part of the invisible continue or the next topic after that
                    currentTopic.Responses.RemoveRange(indexOf, currentTopic.Responses.Count - indexOf);
                }
            }
        }
    }
}