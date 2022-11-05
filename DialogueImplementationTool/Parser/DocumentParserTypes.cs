using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Noggog;
namespace DialogueImplementationTool.Parser; 

public class GeneratedDialogue {
    public GeneratedDialogue(DialogueType type, List<DialogueTopic> topics, FormKey speakerFormKey) {
        Type = type;
        Topics = topics;

        var speaker = new Speaker(speakerFormKey);
        
        //Set speaker for all linked topics
        var linkedTopics = new Queue<DialogueTopic>(topics);
        while (linkedTopics.Any()) {
            var topic = linkedTopics.Dequeue();
            linkedTopics.Enqueue(topic.Links);
            topic.Speaker = speaker;
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

        Selection[DialogueType.GenericScene] = true;
    }
    
    public readonly Dictionary<DialogueType, bool> Selection = Enum.GetValues<DialogueType>().ToDictionary(type => type, _ => false);
    public FormKey Speaker { get; set; } = FormKey.Null;
}