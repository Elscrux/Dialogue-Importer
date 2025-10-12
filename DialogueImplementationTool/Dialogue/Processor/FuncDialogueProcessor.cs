using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class FuncDialogueProcessor(IDialogueProcessor processor) : IDialogueProcessor {
    public Action<GenericDialogue, DialogueTopicInfo, Action<GenericDialogue, DialogueTopicInfo>> ProcessGenericDialogue { get; init; }
        = (genericDialogue, dialogueTopicInfo, baseProcess) => baseProcess(genericDialogue, dialogueTopicInfo);

    public Action<DialogueTopicInfo, Action<DialogueTopicInfo>> PreProcessTopicInfo { get; init; }
        = (topicInfo, baseProcess) => baseProcess(topicInfo);

    public Action<DialogueResponse, IList<FormattedText>,
        Action<DialogueResponse, IList<FormattedText>>> ProcessResponse { get; init; }
        = (response, textSnippets, baseProcess) => baseProcess(response, textSnippets);

    public Action<DialogueTopic, Action<DialogueTopic>> ProcessTopic { get; init; }
        = (topic, baseProcess) => baseProcess(topic);

    public Action<List<DialogueTopic>, Action<List<DialogueTopic>>> ProcessTopics { get; init; }
        = (topics, baseProcess) => baseProcess(topics);

    public Action<Conversation, Action<Conversation>> ProcessConversation { get; init; }
        = (dialogue, baseProcess) => baseProcess(dialogue);

    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        ProcessGenericDialogue(genericDialogue, topicInfo, processor.Process);
    }

    public void Process(DialogueTopicInfo topicInfo) {
        PreProcessTopicInfo(topicInfo, processor.Process);
    }

    public void Process(DialogueResponse response, IList<FormattedText> textSnippets) {
        ProcessResponse(response, textSnippets, processor.Process);
    }

    public void Process(DialogueTopic topic) {
        ProcessTopic(topic, processor.Process);
    }

    public void Process(List<DialogueTopic> topics) {
        ProcessTopics(topics, processor.Process);
    }

    public void Process(Conversation conversation) {
        ProcessConversation(conversation, processor.Process);
    }
}
