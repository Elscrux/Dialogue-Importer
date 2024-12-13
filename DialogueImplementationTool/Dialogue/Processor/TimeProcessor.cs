using DialogueImplementationTool.Converter;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TimeProcessor : IGenericDialogueProcessor {
    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        if (genericDialogue.Time is null) return;

        var conditions = TimeConditionConverter.Convert(genericDialogue.Time);
        topicInfo.ExtraConditions.AddRange(conditions);
    }
}
