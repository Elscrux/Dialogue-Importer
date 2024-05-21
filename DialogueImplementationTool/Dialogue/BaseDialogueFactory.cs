﻿using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public abstract class BaseDialogueFactory(IDialogueContext context) {
    protected readonly IDialogueContext Context = context;

    public abstract void PreProcess(List<DialogueTopic> topics);
    public abstract void GenerateDialogue(List<DialogueTopic> topics);
    public abstract void PostProcess();

    public static BaseDialogueFactory GetBaseFactory(DialogueType type, IDialogueContext context) {
        return type switch {
            DialogueType.Dialogue => new DialogueFactory(context),
            DialogueType.Greeting => new GreetingFactory(context),
            DialogueType.Farewell => new FarewellFactory(context),
            DialogueType.Idle => new IdleFactory(context),
            DialogueType.GenericScene => new GenericGenericScene3X3Factory(context),
            DialogueType.QuestScene => new QuestGenericScene3X3Factory(context),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    public static IEnumerable<GeneratedDialogue> PrepareDialogue(
        IDialogueContext context,
        DialogueProcessor dialogueProcessor,
        IDocumentParser documentParser,
        DialogueSelection selection,
        int index) {
        foreach (var type in selection.SelectedTypes) {
            // Setup factory and factory specific processing
            var factory = GetBaseFactory(type, context);
            var factorySpecificProcessor = factory.ConfigureProcessor(dialogueProcessor);

            // Parse document
            var topics = documentParser.Parse(type, factorySpecificProcessor, index);

            // Use more specific factory if needed
            factory = factory.SpecifyType(topics);
            factorySpecificProcessor = factory.ConfigureProcessor(dialogueProcessor);

            // Process topic and topic infos
            foreach (var topic in topics.EnumerateLinks(true)) {
                factorySpecificProcessor.Process(topic);

                foreach (var topicInfo in topic.TopicInfos) {
                    factorySpecificProcessor.PostProcess(topicInfo);
                }
            }

            factorySpecificProcessor.Process(topics);

            yield return new GeneratedDialogue(
                context,
                factory,
                topics,
                selection.Speaker,
                selection.UseGetIsAliasRef);
        }
    }

    public static Conversation PrepareDialogue(
        IDialogueContext context,
        DialogueProcessor dialogueProcessor,
        IDocumentParser documentParser,
        List<DialogueSelection> dialogueSelections) {
        var conversation = new Conversation();
        for (var i = 0; i < dialogueSelections.Count; i++) {
            var selection = dialogueSelections[i];
            conversation.AddRange(PrepareDialogue(context, dialogueProcessor, documentParser, selection, i));
        }

        return conversation;
    }

    public virtual BaseDialogueFactory SpecifyType(List<DialogueTopic> topics) => this;

    public virtual IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) => dialogueProcessor;

    public void Create(GeneratedDialogue generatedDialogue) {
        if (generatedDialogue.Topics.Count == 0) return;

        PreProcess(generatedDialogue.Topics);
        GenerateDialogue(generatedDialogue.Topics);
        PostProcess();
    }

    protected static Condition GetFormKeyCondition(
        ConditionData data,
        float comparisonValue = 1,
        bool or = false) {
        var condition = new ConditionFloat {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = comparisonValue,
            Data = data,
        };

        if (or) condition.Flags = Condition.Flag.OR;

        return condition;
    }

    protected ExtendedList<DialogResponses> GetTopicInfos(IQuest quest, DialogueTopic topic) {
        return topic.TopicInfos.Select(info => GetResponses(quest, info)).ToExtendedList();
    }

    public DialogResponses GetResponses(IQuest quest, DialogueTopicInfo topicInfo, FormKey? previousDialogue = null) {
        var previousDialog = new FormLinkNullable<IDialogResponsesGetter>(previousDialogue ?? FormKey.Null);

        var flags = new DialogResponseFlags();

        if (topicInfo.SayOnce) flags.Flags |= DialogResponses.Flag.SayOnce;
        if (topicInfo.Goodbye) flags.Flags |= DialogResponses.Flag.Goodbye;
        if (topicInfo.InvisibleContinue) flags.Flags |= DialogResponses.Flag.InvisibleContinue;
        if (topicInfo.Random) flags.Flags |= DialogResponses.Flag.Random;

        if (topicInfo.SharedInfo is not null) {
            var dialogResponses =
                topicInfo.SharedInfo.GetResponseData(quest, Context, TopicInfos, GetConditions);
            dialogResponses.PreviousDialog = previousDialog;
            dialogResponses.Flags = flags;

            return dialogResponses;
        }

        return new DialogResponses(Context.GetNextFormKey(), Context.Release) {
            Responses = TopicInfos(topicInfo).ToExtendedList(),
            Prompt = topicInfo.Prompt.IsNullOrWhitespace() ? null : topicInfo.Prompt,
            Conditions = GetConditions(topicInfo),
            FavorLevel = FavorLevel.None,
            Flags = flags,
            PreviousDialog = previousDialog,
        };

        static IEnumerable<DialogResponse> TopicInfos(DialogueTopicInfo info) {
            return info.Responses.Select((line, i) => new DialogResponse {
                Text = line.FullResponse,
                ScriptNotes = line.ScriptNote,
                ResponseNumber = (byte) (i + 1), //Starts with 1
                Flags = DialogResponse.Flag.UseEmotionAnimation,
                Emotion = line.Emotion,
                EmotionValue = line.EmotionValue,
            });
        }
    }

    public ExtendedList<Condition> GetConditions(DialogueTopicInfo topicInfo) {
        var list = new ExtendedList<Condition>();

        if (topicInfo.Speaker is AliasSpeaker aliasSpeaker) {
            list.Add(new ConditionFloat {
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1,
                Data = new GetIsAliasRefConditionData {
                    ReferenceAliasIndex = aliasSpeaker.AliasIndex,
                },
            });
        } else if (Context.LinkCache.TryResolve<INpcGetter>(topicInfo.Speaker.FormKey, out var npc)) {
            var data = new GetIsIDConditionData();
            data.Object.Link.SetTo(npc.FormKey);
            list.Add(GetFormKeyCondition(data));
        } else if (Context.LinkCache.TryResolve<IFactionGetter>(topicInfo.Speaker.FormKey, out var faction)) {
            var data = new GetInFactionConditionData();
            data.Faction.Link.SetTo(faction.FormKey);
            list.Add(GetFormKeyCondition(data));
        } else if (Context.LinkCache.TryResolve<IVoiceTypeGetter>(topicInfo.Speaker.FormKey, out var voiceType)) {
            var data = new GetIsVoiceTypeConditionData();
            data.VoiceTypeOrList.Link.SetTo(voiceType.FormKey);
            list.Add(GetFormKeyCondition(data));
        } else if (Context.LinkCache.TryResolve<IFormListGetter>(topicInfo.Speaker.FormKey, out var formList)) {
            var data = new GetIsVoiceTypeConditionData();
            data.VoiceTypeOrList.Link.SetTo(formList.FormKey);
            list.Add(GetFormKeyCondition(data));
        }

        list.AddRange(topicInfo.ExtraConditions);

        return list;
    }
}