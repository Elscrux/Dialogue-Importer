using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public abstract class BaseDialogueFactory(IDialogueContext context) {
    protected readonly IDialogueContext Context = context;

    public abstract void PreProcess(List<DialogueTopic> topics);
    public abstract void GenerateDialogue(List<DialogueTopic> topics);

    public static BaseDialogueFactory GetBaseFactory(DialogueType type, IDialogueContext context) {
        return type switch {
            DialogueType.Dialogue => new DialogueFactory(context),
            DialogueType.Greeting => new GreetingFactory(context),
            DialogueType.Farewell => new FarewellFactory(context),
            DialogueType.Idle => new IdleFactory(context),
            DialogueType.GenericScene => new GenericGenericSceneFactory(context),
            DialogueType.QuestScene => new QuestSceneFactory(context),
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
            var processor = dialogueProcessor.Clone();

            // Setup factory and factory specific processing
            var factory = GetBaseFactory(type, context);
            var factorySpecificProcessor = factory.ConfigureProcessor(processor);

            // Parse document
            var topics = documentParser.Parse(type, factorySpecificProcessor, index);

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

        // Handle flags
        if (topicInfo.SayOnce) flags.Flags |= DialogResponses.Flag.SayOnce;
        if (topicInfo.Goodbye) flags.Flags |= DialogResponses.Flag.Goodbye;
        if (topicInfo.InvisibleContinue) flags.Flags |= DialogResponses.Flag.InvisibleContinue;
        if (topicInfo.Random) flags.Flags |= DialogResponses.Flag.Random;
        if (topicInfo.ResetHours is > 0 and <= 24) flags.ResetHours = topicInfo.ResetHours;

        // Handle shared info
        if (topicInfo.SharedInfo is not null) {
            var dialogResponses =
                topicInfo.SharedInfo.GetResponseData(quest, Context, TopicInfos, GetConditions);
            dialogResponses.PreviousDialog = previousDialog;
            dialogResponses.Flags = flags;

            return dialogResponses;
        }

        // Handle responses
        var responses = new DialogResponses(Context.GetNextFormKey(), Context.Release) {
            Responses = TopicInfos(topicInfo).ToExtendedList(),
            Prompt = topicInfo.Prompt.FullText.IsNullOrWhitespace() ? null : topicInfo.Prompt.FullText,
            Conditions = GetConditions(topicInfo),
            FavorLevel = FavorLevel.None,
            Flags = flags,
            PreviousDialog = previousDialog,
        };

        // Handle scripts
        if (topicInfo.Script.ScriptLines.Count > 0) {
            var propertyLine = topicInfo.Script.Properties
                .Select(property => $"{property.ScriptName} Property {property.ScriptProperty.Name} Auto")
                .ToList();

            var scriptName = $"{Context.Prefix}_TIF__{responses.FormKey.ToFormID(Context.Mod, Context.LinkCache)}";
            var scriptText = $"""
            ;BEGIN FRAGMENT CODE - Do not edit anything between this and the end comment
            ;NEXT FRAGMENT INDEX 1
            Scriptname {scriptName} Extends TopicInfo Hidden
            
            ;BEGIN FRAGMENT Fragment_0
            Function Fragment_0(ObjectReference akSpeakerRef)
            Actor akSpeaker = akSpeakerRef as Actor
            ;BEGIN CODE
            {string.Join("\r\n", topicInfo.Script.ScriptLines)}
            ;END CODE
            EndFunction
            ;END FRAGMENT
            
            ;END FRAGMENT CODE - Do not edit anything between this and the begin comment
            
            {string.Join("\r\n", propertyLine)}
            """;

            Context.Scripts.Add(scriptName, scriptText);

            responses.VirtualMachineAdapter = new DialogResponsesAdapter {
                Scripts = [
                    new ScriptEntry {
                        Name = scriptName,
                        Flags = ScriptEntry.Flag.Local,
                        Properties = topicInfo.Script.Properties.Select(x => x.ScriptProperty).ToExtendedList()
                    }
                ],
                ScriptFragments = new ScriptFragments {
                    FileName = scriptName,
                    OnBegin = new ScriptFragment {
                        ExtraBindDataVersion = 2,
                        ScriptName = scriptName,
                        FragmentName = "Fragment_0",
                    },
                },
            };
        }

        return responses;

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
