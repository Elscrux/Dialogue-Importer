using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class FuncDialogueProcessor(IDialogueProcessor processor) : IDialogueProcessor {
    public Action<DialogueTopicInfo, Action<DialogueTopicInfo>> PreProcessTopicInfo { get; init; }
        = (topicInfo, baseProcess) => baseProcess(topicInfo);

    public Action<DialogueResponse, IReadOnlyList<FormattedText>,
        Action<DialogueResponse, IReadOnlyList<FormattedText>>> ProcessResponse { get; init; }
        = (response, textSnippets, baseProcess) => baseProcess(response, textSnippets);

    public Action<DialogueTopicInfo, Action<DialogueTopicInfo>> PostProcessTopicInfo { get; init; }
        = (topicInfo, baseProcess) => baseProcess(topicInfo);

    public Action<DialogueTopic, Action<DialogueTopic>> ProcessTopic { get; init; }
        = (topic, baseProcess) => baseProcess(topic);

    public Action<List<DialogueTopic>, Action<List<DialogueTopic>>> ProcessTopics { get; init; }
        = (topics, baseProcess) => baseProcess(topics);

    public Action<Conversation, Action<Conversation>> ProcessConversation { get; init; }
        = (dialogue, baseProcess) => baseProcess(dialogue);

    public void PreProcess(DialogueTopicInfo topicInfo) {
        PreProcessTopicInfo(topicInfo, processor.PreProcess);
    }

    public void PostProcess(DialogueTopicInfo topicInfo) {
        PostProcessTopicInfo(topicInfo, processor.PostProcess);
    }

    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
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
