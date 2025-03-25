using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
namespace DialogueImplementationTool.Parser;

public interface IDocumentIterator : IDocumentParser {
    int Index { get; set; }

    int LastIndex { get; }

    void Previous() {
        if (Index > 0) Index--;
    }

    void Next() {
        if (Index < LastIndex) Index++;
    }

    void SkipMany();
    void BacktrackMany();

    string PreviewCurrent() {
        return Preview(Index);
    }

    string Preview(int index);

    IEnumerable<GeneratedDialogue> ParseDialogue(
        IDialogueContext context,
        DialogueProcessor dialogueProcessor,
        DialogueSelection selection,
        int index) {
        foreach (var type in selection.SelectedTypes) {
            var processor = dialogueProcessor.Clone();

            // Setup factory and factory specific processing
            var factory = BaseDialogueFactory.GetBaseFactory(type, context);
            var factorySpecificProcessor = factory.ConfigureProcessor(processor);

            // Parse document
            var topics = type switch {
                DialogueType.Dialogue
                    when this is IBranchingDialogueParser branchingDialogueParser =>
                    branchingDialogueParser.ParseBranchingDialogue(factorySpecificProcessor, index),
                DialogueType.Greeting or DialogueType.Farewell or DialogueType.Idle
                    when this is IOneLinerParser oneLinerParser => oneLinerParser.ParseOneLiner(factorySpecificProcessor, index),
                DialogueType.GenericScene or DialogueType.QuestScene
                    when this is ISceneParser sceneParser => sceneParser.ParseScene(factorySpecificProcessor, index),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

            // Set speaker
            ISpeaker speaker;
            if (selection.UseGetIsAliasRef) {
                var alias = context.Quest.GetOrAddAliasUniqueActor(context.LinkCache, selection.Speaker.FormKey);
                speaker = new AliasSpeaker(selection.Speaker, alias.Name!, (int) alias.ID);
            } else {
                speaker = new NpcSpeaker(context.LinkCache, selection.Speaker);
            }

            //Set speaker for all linked topics
            foreach (var topic in topics.EnumerateLinks(true)) {
                foreach (var topicInfo in topic.TopicInfos) {
                    topicInfo.Speaker = speaker;
                }
            }

            // Use more specific factory if needed
            factory = factory.SpecifyType(topics);
            factorySpecificProcessor = factory.ConfigureProcessor(processor);

            // Process topic and topic infos
            foreach (var topic in topics.EnumerateLinks(true)) {
                foreach (var topicInfo in topic.TopicInfos) {
                    factorySpecificProcessor.Process(topicInfo);
                }

                factorySpecificProcessor.Process(topic);
            }

            factorySpecificProcessor.Process(topics);

            yield return new GeneratedDialogue(factory, topics);
        }
    }

    Conversation ParseDialogue(
        IDialogueContext context,
        DialogueProcessor dialogueProcessor,
        List<DialogueSelection> dialogueSelections) {
        var conversation = new Conversation();
        for (var i = 0; i < dialogueSelections.Count; i++) {
            var selection = dialogueSelections[i];
            conversation.AddRange(ParseDialogue(context, dialogueProcessor, selection, i));
        }

        // Conversation wide processing
        dialogueProcessor.Process(conversation);

        return conversation;
    }
}
