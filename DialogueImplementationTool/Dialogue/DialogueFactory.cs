using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public abstract class DialogueFactory(IDialogueContext context) {
    protected readonly IDialogueContext Context = context;

    public abstract void PreProcess(List<DialogueTopic> topics);
    public abstract void GenerateDialogue(IQuest quest, List<DialogueTopic> topics);
    public abstract void PostProcess();

    protected static Condition GetFormKeyCondition(
        Condition.Function function,
        FormKey formKey,
        float comparisonValue = 1,
        bool or = false) {
        var condition = new ConditionFloat {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = comparisonValue,
            Data = new FunctionConditionData {
                Function = function,
                ParameterOneRecord = new FormLink<ISkyrimMajorRecordGetter>(formKey),
            },
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
                topicInfo.SharedInfo.GetResponseData(quest, Context, GetResponses, GetSpeakerConditions);
            dialogResponses.PreviousDialog = previousDialog;
            dialogResponses.Flags = flags;

            return dialogResponses;
        }

        return new DialogResponses(Context.GetNextFormKey(), Context.Release) {
            Responses = topicInfo.Responses.Select((line, i) => new DialogResponse {
                    Text = line.Response,
                    ScriptNotes = line.ScriptNote,
                    ResponseNumber = (byte) (i + 1), //Starts with 1
                    Flags = DialogResponse.Flag.UseEmotionAnimation,
                    Emotion = line.Emotion,
                    EmotionValue = line.EmotionValue,
                })
                .ToExtendedList(),
            Prompt = topicInfo.Prompt.IsNullOrWhitespace() ? null : topicInfo.Prompt,
            Conditions = GetSpeakerConditions(topicInfo.Speaker),
            FavorLevel = FavorLevel.None,
            Flags = flags,
            PreviousDialog = previousDialog,
        };
    }

    public ExtendedList<Condition> GetSpeakerConditions(ISpeaker speaker) {
        var list = new ExtendedList<Condition>();

        if (speaker is AliasSpeaker aliasSpeaker)
            list.Add(new ConditionFloat {
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1,
                Data = new FunctionConditionData {
                    Function = Condition.Function.GetIsAliasRef,
                    ParameterOneNumber = aliasSpeaker.AliasIndex,
                },
            });
        else if (Context.LinkCache.TryResolve<INpcGetter>(speaker.FormKey, out var npc))
            list.Add(GetFormKeyCondition(Condition.Function.GetIsID, npc.FormKey));
        else if (Context.LinkCache.TryResolve<IFactionGetter>(speaker.FormKey, out var faction))
            list.Add(GetFormKeyCondition(Condition.Function.GetInFaction, faction.FormKey));
        else if (Context.LinkCache.TryResolve<IVoiceTypeGetter>(speaker.FormKey, out var voiceType))
            list.Add(GetFormKeyCondition(Condition.Function.GetIsVoiceType, voiceType.FormKey));
        else if (Context.LinkCache.TryResolve<IFormListGetter>(speaker.FormKey, out var formList))
            list.Add(GetFormKeyCondition(Condition.Function.GetIsVoiceType, formList.FormKey));

        return list;
    }
}
