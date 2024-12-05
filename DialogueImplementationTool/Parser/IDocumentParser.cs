using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
namespace DialogueImplementationTool.Parser;

public interface IDocumentParser {
    string FilePath { get; }
}

public interface IBranchingDialogueParser : IDocumentParser {
    List<DialogueTopic> ParseBranchingDialogue(IDialogueProcessor processor, int index);
}

public interface IOneLinerParser : IDocumentParser {
    List<DialogueTopic> ParseOneLiner(IDialogueProcessor processor, int index);
}

public interface ISceneParser : IDocumentParser {
    List<DialogueTopic> ParseScene(IDialogueProcessor processor, int index);
}

public interface IGenericDialogueParser : IDocumentParser {
    List<DialogueTopic> ParseGenericDialogue(IDialogueProcessor processor);
}
