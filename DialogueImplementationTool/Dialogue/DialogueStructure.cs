using System.Collections.Generic;
namespace DialogueImplementationTool.Dialogue; 

public record DialogueTopic {
    public string Text = string.Empty;
    public readonly List<List<string>> Responses = new();
    public readonly List<DialogueTopic> Links = new();
}
