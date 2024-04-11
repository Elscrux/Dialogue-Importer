using DialogueImplementationTool.Dialogue.Model;
namespace DialogueImplementationTool.Dialogue.Processor;

public interface IDialogueResponsePostProcessor {
    public void Process(DialogueResponse response);
}
