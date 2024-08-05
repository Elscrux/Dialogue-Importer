using System.Collections.Generic;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Plugins;
namespace DialogueImplementationTool.Parser;

public sealed record GeneratedDialogue(BaseDialogueFactory Factory, List<DialogueTopic> Topics);

public sealed class DialogueSelection {
    public HashSet<DialogueType> SelectedTypes { get; } = [];
    public FormKey Speaker { get; set; } = FormKey.Null;
    public bool UseGetIsAliasRef { get; set; }
}
