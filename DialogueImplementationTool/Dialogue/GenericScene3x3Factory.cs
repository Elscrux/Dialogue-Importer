﻿using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
namespace DialogueImplementationTool.Dialogue;

public sealed class GenericScene3x3Factory(IDialogueContext context) : GenericGenericSceneFactory(context) {
    public override IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) {
        // Remove trailing colons from prompts (speaker names)
        dialogueProcessor.TopicInfoProcessors.Add(new CustomTopicInfoTrimmer(":"));
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
