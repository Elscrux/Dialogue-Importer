using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class GoldChecker : IDialogueTopicInfoProcessor {
    [GeneratedRegex(@"(\d+) gold added")]
    public static partial Regex GoldAddedRegex();

    [GeneratedRegex(@"(\d+) gold removed")]
    public static partial Regex GoldRemovedRegex();

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            foreach (var note in response.Notes()) {
                if (note.Text is not {} text) continue;

                var removedMatch = GoldRemovedRegex().Match(text);
                if (removedMatch.Success) {
                    var amount = int.Parse(removedMatch.Groups[1].Value);
                    topicInfo.ExtraConditions.Add(new GetItemCountConditionData {
                            RunOnType = Condition.RunOnType.Reference,
                            Reference = Skyrim.PlayerRef,
                            ItemOrList = { Link = { FormKey = Skyrim.MiscItem.Gold001.FormKey } },
                        }
                        .ToConditionFloat(
                            comparisonValue: amount,
                            compareOperator: CompareOperator.GreaterThanOrEqualTo));

                    topicInfo.Script.EndScriptLines.Add($"Game.GetPlayer().RemoveItem(Gold, {amount})");
                    topicInfo.Script.Properties.Add(new ScriptPropertyName(new ScriptObjectProperty {
                            Name = "Gold",
                            Flags = ScriptProperty.Flag.Edited,
                            Object = Skyrim.MiscItem.Gold001,
                        },
                        "MiscObject"));

                    response.RemoveNote(note);
                }
                
                var addedMatch = GoldAddedRegex().Match(text);
                if (addedMatch.Success) {
                    var amount = int.Parse(addedMatch.Groups[1].Value);
                    topicInfo.Script.EndScriptLines.Add($"Game.GetPlayer().AddItem(Gold, {amount})");
                    topicInfo.Script.Properties.Add(new ScriptPropertyName(new ScriptObjectProperty {
                            Name = "Gold",
                            Flags = ScriptProperty.Flag.Edited,
                            Object = Skyrim.MiscItem.Gold001,
                        },
                        "MiscObject"));

                    response.RemoveNote(note);
                }
            }
        }
    }
}
