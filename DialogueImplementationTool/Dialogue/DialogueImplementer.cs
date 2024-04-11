using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class DialogueImplementer {
    private readonly IDialogueContext _context;

    private readonly Dictionary<DialogueType, DialogueFactory> _dialogueFactories;

    public DialogueImplementer(IDialogueContext context) {
        _context = context;
        _dialogueFactories = Enum
            .GetValues<DialogueType>()
            .ToDictionary(type => type,
                type => (DialogueFactory) (type switch {
                    DialogueType.Dialogue => new Dialogue(context),
                    DialogueType.Greeting => new Greeting(context),
                    DialogueType.Farewell => new Farewell(context),
                    DialogueType.Idle => new Idle(context),
                    DialogueType.GenericScene => new GenericScene(context),
                    DialogueType.QuestScene => new QuestScene(context),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
                }));
    }

    public void ImplementDialogue(List<GeneratedDialogue> dialogue) {
        dialogue.Where(d => d.Topics.Count > 0)
            .ForEach(d => _dialogueFactories[d.Type].PreProcess(d.Topics));

        dialogue.Where(d => d.Topics.Count > 0)
            .ForEach(d => _dialogueFactories[d.Type].GenerateDialogue(_context.Quest, d.Topics));

        //Do post processing
        dialogue.Select(d => _dialogueFactories[d.Type])
            .ForEach(d => d.PostProcess());
    }
}
