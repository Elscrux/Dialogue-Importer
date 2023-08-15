﻿using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Topics;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Parser; 

public class GeneratedDialogue {
    public GeneratedDialogue(DialogueType type, List<DialogueTopic> topics, FormKey speakerFormKey, bool useGetIsAliasRef = false) {
        Type = type;
        Topics = topics;

        var alias = DialogueImplementer.OverrideQuest.GetOrAddAlias(DialogueImplementer.Environment.LinkCache, speakerFormKey);
        ISpeaker speaker = useGetIsAliasRef
            ? new AliasSpeaker(alias.Name) {
                FormKey = speakerFormKey,
                AliasIndex = (int) alias.ID
            }
            : new Speaker(speakerFormKey);

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

public sealed class DialogueSelection {
    public readonly Dictionary<DialogueType, bool> Selection = Enum.GetValues<DialogueType>().ToDictionary(type => type, _ => false);
    public FormKey Speaker { get; set; } = FormKey.Null;
    public bool UseGetIsAliasRef { get; set; }
}
