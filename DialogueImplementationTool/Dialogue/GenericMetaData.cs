using System;
using System.Collections.Generic;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public static class GenericMetaData {
    public const string Category = "Category";
    public const string Subtype = "Subtype";
    public const string Description = "Description";
    public const string VoiceType = "VoiceType";
    public const string GenericQuestFactory = "GenericQuestFactory";
    public const string GenericDialogTopicFactory = "GenericDialogTopicFactory";

    public static DialogTopic.CategoryEnum GetCategory(Dictionary<string, object> metaData) {
        if (metaData[Category] is not DialogTopic.CategoryEnum category)
            throw new InvalidOperationException("Category is not set");

        return category;
    }

    public static void SetCategory(Dictionary<string, object> metaData, DialogTopic.CategoryEnum category) {
        metaData[Category] = category;
    }

    public static DialogTopic.SubtypeEnum GetSubtype(Dictionary<string, object> metaData) {
        if (metaData[Subtype] is not DialogTopic.SubtypeEnum subtype)
            throw new InvalidOperationException("Subtype is not set");

        return subtype;
    }

    public static void SetSubtype(Dictionary<string, object> metaData, DialogTopic.SubtypeEnum subtype) {
        metaData[Subtype] = subtype;
    }

    public static string GetDescription(Dictionary<string, object> metaData) {
        if (metaData[Description] is not string description)
            throw new InvalidOperationException("Description is not set");

        return description;
    }

    public static void SetDescription(Dictionary<string, object> metaData, string description) {
        metaData[Description] = description;
    }

    public static VoiceType GetVoiceType(Dictionary<string, object> metaData) {
        if (metaData[VoiceType] is not VoiceType voiceType)
            throw new InvalidOperationException("VoiceType is not set");

        return voiceType;
    }

    public static void SetVoiceType(Dictionary<string, object> metaData, VoiceType voiceType) {
        metaData[VoiceType] = voiceType;
    }

    public static IGenericDialogueQuestFactory GetGenericQuestFactory(Dictionary<string, object> metaData) {
        if (metaData[GenericQuestFactory] is not IGenericDialogueQuestFactory questFactory)
            throw new InvalidOperationException("GenericQuestFactory is not set");

        return questFactory;
    }

    public static void SetGenericQuestFactory(Dictionary<string, object> metaData, IGenericDialogueQuestFactory questFactory) {
        metaData[GenericQuestFactory] = questFactory;
    }

    public static IGenericDialogueTopicFactory GetGenericDialogTopicFactory(Dictionary<string, object> metaData) {
        if (metaData[GenericDialogTopicFactory] is not IGenericDialogueTopicFactory topicFactory)
            throw new InvalidOperationException("GenericDialogTopicFactory is not set");

        return topicFactory;
    }

    public static void SetGenericDialogTopicFactory(
        Dictionary<string, object> metaData,
        IGenericDialogueTopicFactory topicFactory) {
        metaData[GenericDialogTopicFactory] = topicFactory;
    }
}
