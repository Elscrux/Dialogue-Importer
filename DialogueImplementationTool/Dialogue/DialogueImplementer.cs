using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class DialogueImplementer {
    private readonly IDialogueContext _context;
    private readonly Dictionary<DialogueType, Func<DialogueFactory>> _dialogueFactories;

    public DialogueImplementer(IDialogueContext context) {
        _context = context;
        _dialogueFactories = Enum
            .GetValues<DialogueType>()
            .ToDictionary(type => type,
                type => (Func<DialogueFactory>) (type switch {
                    DialogueType.Dialogue => () => new Dialogue(context),
                    DialogueType.Greeting => () => new Greeting(context),
                    DialogueType.Farewell => () => new Farewell(context),
                    DialogueType.Idle => () =>new Idle(context),
                    DialogueType.GenericScene => () => new GenericScene(context),
                    DialogueType.QuestScene => () => new QuestScene(context),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
                }));
    }

    public void ImplementDialogue(List<GeneratedDialogue> dialogue) {
        foreach (var generatedDialogue in dialogue) {
            if (generatedDialogue.Topics.Count == 0) continue;

            var dialogueFactory = _dialogueFactories[generatedDialogue.Type]();
            dialogueFactory.PreProcess(generatedDialogue.Topics);
            dialogueFactory.GenerateDialogue(_context.Quest, generatedDialogue.Topics);
            dialogueFactory.PostProcess();
        }
    }
}
