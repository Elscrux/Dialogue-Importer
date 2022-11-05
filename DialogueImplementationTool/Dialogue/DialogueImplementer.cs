using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Responses;
using DialogueImplementationTool.Dialogue.Topics;
using DialogueImplementationTool.Parser;
using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue; 

public class DialogueImplementer {
    public static readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> Environment = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
    public static IQuestGetter Quest = new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);

    public static DialogueFactory GetDialogueFactory(DialogueType type) {
        return type switch {
            DialogueType.Dialogue => new Dialogue(),
            DialogueType.Greeting => new Greeting(),
            DialogueType.Farewell => new Farewell(),
            DialogueType.Idle => new Idle(),
            DialogueType.GenericScene => new GenericScene(),
            DialogueType.QuestScene => new QuestScene(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public DialogueImplementer(FormKey questFormKey) {
        Quest = questFormKey != FormKey.Null ? Environment.LinkCache.Resolve<IQuestGetter>(questFormKey) : new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);
    }

    public void ImplementDialogue(List<GeneratedDialogue> dialogue) {
        dialogue.Where(d => d.Topics.Count > 0)
            .ForEach(d => d.Factory.PreProcess(d.Topics));
        
        ConvertToSharedLines(dialogue);
        
        dialogue.Where(d => d.Topics.Count > 0)
            .ForEach(d => d.Factory.GenerateDialogue(d.Topics));
        
        //Do post processing
        dialogue.Select(d => d.Factory)
            .ForEach(d => d.PostProcess());
    }
    
    private record SharedLineLink(DialogueTopic TopicUsingLine, SharedLine? Last, SharedLine? Next) {
        public SharedLine? Next { get; set; } = Next;
    }
    
    private record SharedLine : DialogueResponse {
        public SharedLine(DialogueResponse dialogueResponse, FormKey speaker) {
            Response = dialogueResponse.Response;
            ScriptNote = dialogueResponse.ScriptNote;
            Speaker = speaker;
        }
        
        public FormKey Speaker { get; init; }
        public List<SharedLineLink> Users { get; } = new();

        public virtual bool Equals(SharedLine? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return base.Equals(other) && Speaker.Equals(other.Speaker);
        }
        public override int GetHashCode() {
            return HashCode.Combine(base.GetHashCode(), Speaker);
        }
    }
    
    private class CommonSharedLine {
        public CommonSharedLine(SharedLine sharedLine) {
            SharedLines.Add(sharedLine);
        }
        
        public List<SharedLine> SharedLines { get; } = new();
        
        public SharedLine? CommonLast { get; set; }
        public SharedLine? CommonNext { get; set; }
    }

    private static void ConvertToSharedLines(List<GeneratedDialogue> dialogue) { 
        var sharedLines = new HashSet<SharedLine>();

        //Convert dialogue to shared lines, where objects can be shared on a line/response level
        foreach (var generated in dialogue) {
            var linkedTopics = new Queue<DialogueTopic>(generated.Topics);
            
            while (linkedTopics.Any()) {
                var topic = linkedTopics.Dequeue();
                linkedTopics.Enqueue(topic.Links);
                
                SharedLine? last = null;
                SharedLineLink? lastLink = null;
                SharedLine? next = null;
                foreach (var response in topic.Responses) {
                    //Get unique shared line
                    var sharedLine = new SharedLine(response, topic.Speaker.FormKey);
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
        
        //Remove lines that aren't shared
        sharedLines.RemoveWhere(l => l.Users.Count < 2);
        
        //Build a dictionary of all shared lines and potentially their common last or next line
        //depending on where the shared line is used and if they also have a common speaker
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
                if (nextLine is { CommonLast: {} } && nextLine.CommonLast.Equals(current.SharedLines[^1])) {
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
            sharedTopic.Speaker = new Speaker(firstShared.Speaker);
            
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
                    currentTopic.SharedInfo = sharedInfo;

                    var nextRange = currentTopic.Responses.GetRange(sharedTopic.Responses.Count, currentTopic.Responses.Count - sharedTopic.Responses.Count - indexOf);
                    if (nextRange.Count > 0) {
                        var nextTopic = new DialogueTopic {
                            Text = invisibleCont,
                            IncomingLink = currentTopic,
                            Speaker = currentTopic.Speaker,
                        };
                        nextTopic.Responses.AddRange(nextRange);

                        nextTopic.Links.Add(currentTopic.Links);
                        foreach (var dialogueTopic in currentTopic.Links) {
                            dialogueTopic.IncomingLink = nextTopic;
                        }
                        
                        currentTopic.Links.Clear();
                        currentTopic.Links.Add(nextTopic);
                    }
                    
                    currentTopic.Responses.RemoveRange(indexOf + sharedTopic.Responses.Count, currentTopic.Responses.Count - sharedTopic.Responses.Count);
                } else {
                    var invisibleContTopic = new DialogueTopic {
                        Text = invisibleCont,
                        IncomingLink = currentTopic,
                        SharedInfo = sharedInfo,
                        Speaker = currentTopic.Speaker,
                    };
                    invisibleContTopic.Responses.AddRange(sharedTopic.Responses);

                    var nextRange = currentTopic.Responses.GetRange(indexOf + sharedTopic.Responses.Count, currentTopic.Responses.Count - sharedTopic.Responses.Count - indexOf);
                    if (nextRange.Count > 0) {
                        var nextTopic = new DialogueTopic {
                            Text = invisibleCont,
                            IncomingLink = invisibleContTopic,
                            Speaker = currentTopic.Speaker,
                        };
                        nextTopic.Responses.AddRange(nextRange);
                        
                        invisibleContTopic.Links.Add(nextTopic);
                        
                        nextTopic.Links.Add(currentTopic.Links);
                        foreach (var dialogueTopic in currentTopic.Links) {
                            dialogueTopic.IncomingLink = nextTopic;
                        }
                    } else {
                        invisibleContTopic.Links.Add(currentTopic.Links);
                        foreach (var dialogueTopic in currentTopic.Links) {
                            dialogueTopic.IncomingLink = invisibleContTopic;
                        }
                    }
                    
                    currentTopic.Responses.RemoveRange(indexOf, currentTopic.Responses.Count - indexOf);
                    currentTopic.Links.Clear();
                    currentTopic.Links.Add(invisibleContTopic);
                }
            }
        }
    }
}