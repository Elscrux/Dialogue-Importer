using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class GenericScene3x3Factory(IDialogueContext context) : GenericSceneFactory(context) {
    public override IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) {
        // Remove trailing colons from prompts (speaker names)
        dialogueProcessor.TopicInfoProcessors.Add(new CustomTopicInfoTrimmer(":"));
        // Skip pre-processing of regular scene factory
        return dialogueProcessor;
    }

    protected override void TransformLines(List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            topic.ConvertResponsesToTopicInfos();
            var randomChecker = new RandomChecker();
            var belongsToPreviousChecker = new BelongsToPreviousChecker();
            randomChecker.Process(topic);
            belongsToPreviousChecker.Process(topic);

            foreach (var topicInfo in topic.TopicInfos) {
                topicInfo.Random = true;
            }
        }
    }
}
