﻿using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Parser; 

public class GeneratedDialogue {
    public GeneratedDialogue(DialogueType type, List<DialogueTopic> topics, FormKey speakerFormKey) {
        Type = type;
        Topics = topics;

        var speaker = new Speaker(speakerFormKey);
        
        //Set speaker for all linked topics
        foreach (var rootTopic in topics) {
            foreach (var topic in rootTopic.EnumerateLinks()) {
                topic.Speaker = speaker;
            }
        }

        Factory = DialogueImplementer.GetDialogueFactory(Type);
    }
    
    public DialogueFactory Factory { get; }
    public DialogueType Type { get; }
    public List<DialogueTopic> Topics { get; }

}

public record DialogueSelection {
    public void Deconstruct(out Dictionary<DialogueType, bool> selection, out FormKey speakerFormKey) {
        selection = Selection;
        speakerFormKey = Speaker;
    }
    
    public readonly Dictionary<DialogueType, bool> Selection = Enum.GetValues<DialogueType>().ToDictionary(type => type, _ => false);
    public FormKey Speaker { get; set; } = FormKey.Null;
}