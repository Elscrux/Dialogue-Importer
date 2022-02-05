using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue; 

public abstract class DialogueFactory {
    protected const SkyrimRelease Release = SkyrimRelease.SkyrimSE;
    public static readonly SkyrimMod Mod = new(new ModKey("DialogueOutput", ModType.Plugin), SkyrimRelease.SkyrimSE);

    protected IQuest? OverrideQuest = null;

    public abstract void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, string speakerName);

    public static void Save() {
        var index = 1;
        var fileInfo = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", Mod.ModKey.FileName + index));
        while (fileInfo.Exists) {
            index++;
            fileInfo = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", Mod.ModKey.FileName + index));
        }
        
        if (!fileInfo.Exists) fileInfo.Directory?.Create();
        Mod.WriteToBinaryParallel(fileInfo.FullName);
    }

    protected static Condition GetIsIDCondition(FormKey npc, bool or = false) {
        var condition = new ConditionFloat {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = 1,
            Data = new FunctionConditionData {
                Function = Condition.Function.GetIsID,
                ParameterOneRecord = new FormLink<ISkyrimMajorRecordGetter>(npc)
            }
        };

        if (or) condition.Flags = Condition.Flag.OR;

        return condition;
    }

    protected static ExtendedList<DialogResponses> GetResponses(FormKey npc, IEnumerable<string> lines) {
        return new ExtendedList<DialogResponses> { 
            new(Mod.GetNextFormKey(), Release) {
                Responses = lines.Select((response, i) => new DialogResponse {
                    Text = response,
                    ResponseNumber = (byte) i,
                    Flags = DialogResponse.Flag.UseEmotionAnimation,
                    EmotionValue = 50
                }).ToExtendedList(),
                Conditions = new ExtendedList<Condition> { GetIsIDCondition(npc) },
                FavorLevel = FavorLevel.None,
                Flags = new DialogResponseFlags(),
                PreviousDialog = new FormLinkNullable<IDialogResponsesGetter>(FormKey.Null)
            }
        };
    }
}
