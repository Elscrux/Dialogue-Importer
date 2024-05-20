using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
namespace DialogueImplementationTool.Parser;

public interface IDocumentParser : IDocumentIterator {
    public List<DialogueTopic> Parse(DialogueType type, IDialogueProcessor processor, int index) {
        return type switch {
            DialogueType.Dialogue => ParseDialogue(processor, index),
            DialogueType.Greeting or DialogueType.Farewell or DialogueType.Idle => ParseOneLiner(processor, index),
            DialogueType.GenericScene or DialogueType.QuestScene => ParseScene(processor, index),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    List<DialogueTopic> ParseDialogue(IDialogueProcessor processor, int index);
    List<DialogueTopic> ParseOneLiner(IDialogueProcessor processor, int index);
    List<DialogueTopic> ParseScene(IDialogueProcessor processor, int index) => ParseDialogue(processor, index);
}
