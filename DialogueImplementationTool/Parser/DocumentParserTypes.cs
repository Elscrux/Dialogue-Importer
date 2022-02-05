using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Parser; 

public record GeneratedDialogue(DialogueType Type, List<DialogueTopic> Topics, FormKey SpeakerFormKey);

public record DialogueSelection {
    public void Deconstruct(out Dictionary<DialogueType, bool> selection, out FormKey speakerFormKey) {
        selection = Selection;
        speakerFormKey = Speaker;
    }
    
    public readonly Dictionary<DialogueType, bool> Selection = Enum.GetValues<DialogueType>().ToDictionary(type => type, _ => false);
    public FormKey Speaker { get; set; } = FormKey.Null;
}

public record SpeakerFavourite(FormKey FormKey, string? EditorID) {
    public FormKey FormKey { get; set; } = FormKey;
    public string? EditorID { get; set; } = EditorID;
}