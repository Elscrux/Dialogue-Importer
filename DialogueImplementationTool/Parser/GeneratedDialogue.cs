using System.Collections.Generic;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Parser;

public sealed record GeneratedDialogue(BaseDialogueFactory Factory, List<DialogueTopic> Topics);

public sealed class DialogueSelection {
    public HashSet<DialogueType> SelectedTypes { get; } = [];
    public IFormLinkGetter Speaker { get; set; } = new FormLinkInformation(FormKey.Null, typeof(INpcGetter));
    public bool UseGetIsAliasRef { get; set; }
}
