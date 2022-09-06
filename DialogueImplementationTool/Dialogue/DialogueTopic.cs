using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Responses;
namespace DialogueImplementationTool.Dialogue; 

public record DialogueTopic {
    public string Text = string.Empty;
    public readonly List<DialogueResponse> Responses = new();
    public readonly List<DialogueTopic> Links = new();
}