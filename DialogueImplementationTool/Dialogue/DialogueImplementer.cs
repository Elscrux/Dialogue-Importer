using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    private static readonly Regex WhitespaceRegex = new(@"\s+");
    public static IQuestGetter Quest = new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);

    public static readonly Dictionary<FormKey, string> NameMappings = new();

    private static readonly Dictionary<DialogueType, DialogueFactory> DialogueFactories = new() {
        { DialogueType.Greeting, new Greeting() },
        { DialogueType.Farewell, new Farewell() },
        { DialogueType.Idle, new Idle() },
        { DialogueType.Dialogue, new Dialogue() },
        { DialogueType.GenericScene, new GenericScene() },
        { DialogueType.QuestScene, new QuestScene() }
    };

    public DialogueImplementer(FormKey questFormKey) {
        Quest = questFormKey != FormKey.Null ? Environment.LinkCache.Resolve<IQuestGetter>(questFormKey) : new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);
    }

    public void ImplementDialogue(List<GeneratedDialogue> dialogue) {
        ConvertToSharedLines(dialogue);
        
        dialogue
            .Where(x => x.Topics.Count > 0 && DialogueFactories.ContainsKey(x.Type))
            .ForEach(x => DialogueFactories[x.Type].GenerateDialogue(x.Topics, x.SpeakerFormKey, GetSpeakerName(x.SpeakerFormKey)));
        
        //Do post processing
        foreach (var factory in DialogueFactories.Values) factory.PostProcess();
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

        // public List<CommonSharedLine> BackLinks { get; set; } = new();
        // public List<CommonSharedLine> ForwardLinks { get; set; } = new();
    }

    private static void ConvertToSharedLines(List<GeneratedDialogue> dialogue) { 
        var sharedLines = new HashSet<SharedLine>();

        //Convert dialogue to shared lines, where objects can be shared on a line/response level
        foreach (var (_, topics, speaker) in dialogue) {
            var linkedTopics = new Queue<DialogueTopic>(topics);
            
            while (linkedTopics.Any()) {
                var topic = linkedTopics.Dequeue();
                linkedTopics.Enqueue(topic.Links);
                
                SharedLine? last = null;
                SharedLineLink? lastLink = null;
                SharedLine? next = null;
                foreach (var response in topic.Responses) {
                    //Get unique shared line
                    var sharedLine = new SharedLine(response, speaker);
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
        
        //Rove lines that aren't shared
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
        // foreach (var current in commonSharedLines.Where(l => l.BackLinks.Count < 2 && l.ForwardLinks.Count < 2)) {
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
        
        const string invisCont = "(invis cont)";
        foreach (var commonSharedLine in commonSharedLines) {
            var firstShared = commonSharedLine.SharedLines[0];  
            
            //Convert common shared lines to shared infos
            var sharedTopic = new DialogueTopic();
            sharedTopic.Responses.AddRange(commonSharedLine.SharedLines);
            
            var sharedInfo = new SharedInfo(firstShared.Speaker, sharedTopic);
            
            //Integrate into dialogue structure and setup all the linking correctly
            foreach (var (topicUsingLine, last, next) in firstShared.Users) {
                var currentTopic = topicUsingLine;
                
                //Search for topics that were nested behind invis conts through shared dialogue
                var indexOf = currentTopic.SharedInfo == null ? currentTopic.Responses.IndexOf(firstShared) : -1;
                while (indexOf == -1 && currentTopic.Links.Count == 1 && currentTopic.Links[0].Text == invisCont) {
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
                            Text = invisCont,
                            IncomingLink = currentTopic,
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
                        Text = invisCont,
                        IncomingLink = currentTopic,
                        SharedInfo = sharedInfo,
                    };
                    invisibleContTopic.Responses.AddRange(sharedTopic.Responses);

                    var nextRange = currentTopic.Responses.GetRange(indexOf + sharedTopic.Responses.Count, currentTopic.Responses.Count - sharedTopic.Responses.Count - indexOf);
                    if (nextRange.Count > 0) {
                        var nextTopic = new DialogueTopic {
                            Text = invisCont,
                            IncomingLink = invisibleContTopic,
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

    private static string GetSpeakerName(FormKey speaker) {
        var name = string.Empty;
        
        if (speaker != FormKey.Null) {
            if (NameMappings.TryGetValue(speaker, out var speakerName)) {
                name = speakerName;
            } else {
                if (Environment.LinkCache.TryResolve<INpcGetter>(speaker, out var named)) {
                    //Remove white spaces from name
                    name = WhitespaceRegex.Replace(named.Name?.String ?? string.Empty, string.Empty);

                    NameMappings.Add(named.FormKey, name);
                }
            }
        }
        
        return name;
    }
}