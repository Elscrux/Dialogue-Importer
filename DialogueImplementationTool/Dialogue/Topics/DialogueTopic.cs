using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Responses;
namespace DialogueImplementationTool.Dialogue.Topics; 

public record DialogueTopic {
    private static readonly IEnumerable<IDialogueTopicPostProcessor> PreProcessors = new List<IDialogueTopicPostProcessor> {
        new SayOnceChecker(),
        new Trimmer(),
        new InvalidStringFixer(),
    };
    
    private static readonly IEnumerable<IDialogueTopicPostProcessor> PostProcessors = new List<IDialogueTopicPostProcessor> {
        new BackToOptionsLinker(),
    };
    
    public SharedInfo? SharedInfo { get; set; }
    
    public ISpeaker Speaker { get; set; }
    
    public string Text { get; set; } = string.Empty;
    public readonly List<DialogueResponse> Responses = new();
    public readonly List<DialogueTopic> Links = new();
    public DialogueTopic? IncomingLink { get; set; }
    public bool SayOnce { get; set; }

    public void Build() {
        foreach (var preProcessor in PreProcessors) {
            preProcessor.Process(this);
        }
    }

    public void PostProcess() {
        foreach (var postProcessor in PostProcessors) {
            postProcessor.Process(this);
        }
    }
    
    public IEnumerable<DialogueTopic> EnumerateLinks() {
        yield return this;

        var returnedLinks = new HashSet<DialogueTopic>();

        var queue = new Queue<DialogueTopic>(Links);
        while (queue.Any()) {
            var dialogueTopic = queue.Dequeue();
            if (returnedLinks.Contains(dialogueTopic)) continue;

            returnedLinks.Add(dialogueTopic);
            foreach (var link in dialogueTopic.Links) {
                queue.Enqueue(link);
            }
            yield return dialogueTopic;
        }
    }
}