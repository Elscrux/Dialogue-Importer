using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Responses;
namespace DialogueImplementationTool.Dialogue.Topics; 

public record DialogueTopic {
    private static readonly IEnumerable<IDialogueTopicPostProcessor> PostProcessors = new List<IDialogueTopicPostProcessor> {
        new Trimmer(),
        new InvalidStringFixer(),
    };
    
    public string Text {
        get => _text;
        set {
            _text = value;
            foreach (var preProcessor in PostProcessors) {
                preProcessor.Process(this);
            }
        }
    }
    
    public readonly List<DialogueResponse> Responses = new();
    public readonly List<DialogueTopic> Links = new();
    private string _text = string.Empty;
}