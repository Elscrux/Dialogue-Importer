using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue; 

public abstract class DialogueFactory {
    public const SkyrimRelease Release = SkyrimRelease.SkyrimSE;
    public static readonly SkyrimMod Mod = new(new ModKey(GetNewModName(), ModType.Plugin), SkyrimRelease.SkyrimSE);
    private const string ModName = "DialogueOutput";
    public static readonly string OutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

    protected IQuest? OverrideQuest = null;

    private static string GetNewModName() {
        var index = 1;
        var fileInfo = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", $"{ModName}{index}.esp"));
        while (fileInfo.Exists) {
            index++;
            fileInfo = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", $"{ModName}{index}.esp"));
        }

        return ModName + index;
    }

    public abstract void PreProcess(List<DialogueTopic> topics);
    public abstract void GenerateDialogue(List<DialogueTopic> topics);
    public abstract void PostProcess();

    public static void Save() {
        var fileInfo = new FileInfo(Path.Combine(OutputFolder, Mod.ModKey.FileName));
        
        if (fileInfo.Directory is { Exists: false }) fileInfo.Directory?.Create();
        Mod.WriteToBinaryParallel(fileInfo.FullName);
    }

    protected static Condition GetFormKeyCondition(Condition.Function function, FormKey formKey, float comparisonValue = 1, bool or = false) {
        var condition = new ConditionFloat {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = comparisonValue,
            Data = new FunctionConditionData {
                Function = function,
                ParameterOneRecord = new FormLink<ISkyrimMajorRecordGetter>(formKey)
            }
        };

        if (or) condition.Flags = Condition.Flag.OR;

        return condition;
    }

    protected static ExtendedList<DialogResponses> GetResponsesList(DialogueTopic topic) {
        return new ExtendedList<DialogResponses> { GetResponses(topic) };
    }

    public static DialogResponses GetResponses(DialogueTopic topic, FormKey? previousDialogue = null) {
        var previousDialog = new FormLinkNullable<IDialogResponsesGetter>(previousDialogue ?? FormKey.Null);
        
        if (topic.SharedInfo != null) {
            var dialogResponses = topic.SharedInfo.GetResponseData();
            dialogResponses.PreviousDialog = previousDialog;
            
            return dialogResponses;
        }

        var flags = new DialogResponseFlags();

        if (topic.SayOnce) {
            flags.Flags |= DialogResponses.Flag.SayOnce;
        }

        return new DialogResponses(Mod.GetNextFormKey(), Release) {
            Responses = topic.Responses.Select((line, i) => new DialogResponse {
                Text = line.Response,
                ScriptNotes = line.ScriptNote,
                ResponseNumber = (byte) (i + 1), //Starts with 1
                Flags = DialogResponse.Flag.UseEmotionAnimation,
                EmotionValue = 50
            }).ToExtendedList(),
            Conditions = GetSpeakerConditions(topic.Speaker),
            FavorLevel = FavorLevel.None,
            Flags = flags,
            PreviousDialog = previousDialog
        };
    }

    public static ExtendedList<Condition> GetSpeakerConditions(ISpeaker speaker) {
        var list = new ExtendedList<Condition>();

        if (DialogueImplementer.Environment.LinkCache.TryResolve<INpcGetter>(speaker.FormKey, out var npc)) {
            list.Add(GetFormKeyCondition(Condition.Function.GetIsID, npc.FormKey));
        }

        if (DialogueImplementer.Environment.LinkCache.TryResolve<IFactionGetter>(speaker.FormKey, out var faction)) {
            list.Add(GetFormKeyCondition(Condition.Function.GetInFaction, faction.FormKey));
        }

        if (DialogueImplementer.Environment.LinkCache.TryResolve<IVoiceTypeGetter>(speaker.FormKey, out var voiceType)) {
            list.Add(GetFormKeyCondition(Condition.Function.GetIsVoiceType, voiceType.FormKey));
        }

        if (DialogueImplementer.Environment.LinkCache.TryResolve<IFormListGetter>(speaker.FormKey, out var formList)) {
            list.Add(GetFormKeyCondition(Condition.Function.GetIsVoiceType, formList.FormKey));
        }

        return list;
    }

    protected static List<DialogueTopic> GetAllTopics(List<DialogueTopic> topics) {
        //Through shared dialogue detection, a topic that was previously only one topic might be split into multiple topics
        //This is basically flattening the dialogue tree
        var allTopics = new List<DialogueTopic>(topics);

        foreach (var rootTopic in topics) {
            foreach (var topic in rootTopic.EnumerateLinks()) {
                var indexOf = allTopics.IndexOf(topic);
                for (var i = topic.Links.Count - 1; i >= 0; i--) {
                    allTopics.Insert(indexOf + 1, topic.Links[i]);
                }
            }
        }

        return allTopics;
    }
}
