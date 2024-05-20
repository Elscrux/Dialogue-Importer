using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
namespace DialogueImplementationTool.Dialogue;

public sealed class GenericScene3x3Factory(IDialogueContext context) : GenericGenericScene3X3Factory(context) {
    public override IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) {
        // Skip pre-processing of regular scene factory
        return dialogueProcessor;
    }

    protected override void TransformLines(List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            topic.ConvertResponsesToTopicInfos();

            foreach (var topicInfo in topic.TopicInfos) {
                topicInfo.Random = true;
            }
        }
    }
}
