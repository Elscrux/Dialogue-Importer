using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Responses;
namespace DialogueImplementationTool.Dialogue.Topics; 

public record DialogueTopic {
    private static readonly IEnumerable<IDialogueTopicPostProcessor> PostProcessors = new List<IDialogueTopicPostProcessor> {
        new SayOnceChecker(),
        new BackToOptionsLinker(),
        new Trimmer(),
        new InvalidStringFixer(),
    };
    
    public string Text { get; set; } = string.Empty;
    public readonly List<DialogueResponse> Responses = new();
    public readonly List<DialogueTopic> Links = new();
    public DialogueTopic? IncomingLink { get; set; }
    public bool SayOnce { get; set; }

    public void Build() {
        foreach (var preProcessor in PostProcessors) {
            preProcessor.Process(this);
        }
    }
}