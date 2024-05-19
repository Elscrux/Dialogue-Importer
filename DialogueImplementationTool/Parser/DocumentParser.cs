using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
namespace DialogueImplementationTool.Parser;

public interface IDocumentParser : IDocumentIterator {
    public DialogueProcessor DialogueProcessor { get; }

    public List<GeneratedDialogue> GetDialogue(IDialogueContext context, IReadOnlyList<DialogueSelection> selections) {
        var dialogue = new List<GeneratedDialogue>();
        for (var i = 0; i < selections.Count; i++) {
            var selection = selections[i];
            foreach (var (dialogueType, selected) in selection.Selection) {
                if (!selected) continue;

                var dialogueTopics = ParseDialogue(dialogueType, i);
                foreach (var topic in dialogueTopics.EnumerateLinks()) {
                    DialogueProcessor.Process(topic);

                    foreach (var topicInfo in topic.TopicInfos) {
                        DialogueProcessor.PostProcess(topicInfo);
                    }
                }

                dialogue.Add(
                    new GeneratedDialogue(
                        context,
                        dialogueType,
                        dialogueTopics,
                        selection.Speaker,
                        selection.UseGetIsAliasRef));
            }
        }

        return dialogue;
    }

    private List<DialogueTopic> ParseDialogue(DialogueType dialogueType, int index) {
        return dialogueType switch {
            DialogueType.Dialogue => ParseDialogue(index),
            DialogueType.Greeting or DialogueType.Farewell or DialogueType.Idle => ParseOneLiner(index),
            DialogueType.GenericScene or DialogueType.QuestScene => ParseScene(index),
            _ => throw new ArgumentOutOfRangeException(nameof(dialogueType), dialogueType, null),
        };
    }

    List<DialogueTopic> ParseDialogue(int index);
    List<DialogueTopic> ParseOneLiner(int index);
    List<DialogueTopic> ParseScene(int index) {
        var topics = ParseDialogue(index);
        foreach (var topic in topics) topic.ConvertResponsesToTopicInfos();
        return topics;
    }
}
