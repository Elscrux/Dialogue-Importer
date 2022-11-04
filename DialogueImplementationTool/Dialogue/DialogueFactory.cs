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

    public abstract void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, string speakerName);
    public abstract void PostProcess();

    public static void Save() {
        var fileInfo = new FileInfo(Path.Combine(OutputFolder, Mod.ModKey.FileName));
        
        if (fileInfo.Directory is { Exists: false }) fileInfo.Directory?.Create();
        Mod.WriteToBinaryParallel(fileInfo.FullName);
    }

    public static Condition GetFormKeyCondition(Condition.Function function, FormKey formKey, float comparisonValue = 1, bool or = false) {
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

    public static ExtendedList<DialogResponses> GetResponsesList(FormKey speaker, DialogueTopic topic) {
        return new ExtendedList<DialogResponses> { GetResponses(speaker, topic) };
    }

    public static DialogResponses GetResponses(FormKey speaker, DialogueTopic topic, FormKey? previousDialogue = null) {
        var previousDialog = new FormLinkNullable<IDialogResponsesGetter>(previousDialogue ?? FormKey.Null);
        
        if (topic.SharedInfo != null) {
            var dialogResponses = topic.SharedInfo.GetResponseData();
            dialogResponses.PreviousDialog = previousDialog;
            
            return dialogResponses;
        }

        return new DialogResponses(Mod.GetNextFormKey(), Release) {
            Responses = topic.Responses.Select((line, i) => new DialogResponse {
                Text = line.Response,
                ScriptNotes = line.ScriptNote,
                ResponseNumber = (byte) i,
                Flags = DialogResponse.Flag.UseEmotionAnimation,
                EmotionValue = 50
            }).ToExtendedList(),
            Conditions = GetSpeakerConditions(speaker),
            FavorLevel = FavorLevel.None,
            Flags = new DialogResponseFlags(),
            PreviousDialog = previousDialog
        };
    }

    public static ExtendedList<Condition> GetSpeakerConditions(FormKey speaker) {
        var list = new ExtendedList<Condition>();
        
        if (DialogueImplementer.Environment.LinkCache.TryResolve<INpcGetter>(speaker, out var npc)) {
            list.Add(GetFormKeyCondition(Condition.Function.GetIsID, npc.FormKey));
        }
        
        if(DialogueImplementer.Environment.LinkCache.TryResolve<IFactionGetter>(speaker, out var faction)) {
            list.Add(GetFormKeyCondition(Condition.Function.GetInFaction, faction.FormKey));
        }

        return list;
    }
}
