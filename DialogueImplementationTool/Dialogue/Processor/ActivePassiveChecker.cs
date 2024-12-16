using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class ActivePassiveChecker: IDialogueTopicInfoProcessor {
    [GeneratedRegex("active")]
    private static partial Regex ActiveRegex();

    [GeneratedRegex("passive")]
    private static partial Regex PassiveRegex();

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            foreach (var note in response.Notes()) {
                var activeMatch = ActiveRegex().Match(note.Text);
                Condition condition;
                if (activeMatch.Success) 
                {
                    condition = GetValueCondition(activeMatch, CompareOperator.EqualTo, 1);
                } 
                else {
                    var passiveMatch = PassiveRegex().Match(note.Text);
                    if (!passiveMatch.Success) continue;

                    condition = GetValueCondition(passiveMatch, CompareOperator.EqualTo, 0);
                }

                // apply condition and remove the note
                topicInfo.ExtraConditions.Add(condition);
                response.RemoveNote(note);

            }
        }

        Condition GetValueCondition(Match match, CompareOperator compareOperator, float comparisonValue) {
            
            var isInDialogueWithPlayer = new ConditionFloat 
            {
                Data = new IsInDialogueWithPlayerConditionData(),
                CompareOperator = compareOperator,
                ComparisonValue = comparisonValue
            };
            return isInDialogueWithPlayer;
        }
    }
}
