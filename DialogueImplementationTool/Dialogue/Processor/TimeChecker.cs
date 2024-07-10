using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class TimeChecker : IDialogueTopicInfoProcessor {
    private const string HourPattern = @"(\d{1,2}):(\d{2})";

    [GeneratedRegex(@$"{HourPattern}[^\d]*{HourPattern}")]
    private static partial Regex TimeRegex();

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            foreach (var note in response.Notes()) {
                var match = TimeRegex().Match(note.Text);
                if (!match.Success) continue;

                if (!int.TryParse(match.Groups[1].Value, out var startHour)
                 || !int.TryParse(match.Groups[2].Value, out var startMinutes)
                 || !int.TryParse(match.Groups[3].Value, out var endHour)
                 || !int.TryParse(match.Groups[4].Value, out var endMinutes)) continue;

                var startCondition = GetGlobalValueCondition(startHour, startMinutes);
                startCondition.CompareOperator = CompareOperator.GreaterThanOrEqualTo;
                var endCondition = GetGlobalValueCondition(endHour, endMinutes);
                endCondition.CompareOperator = CompareOperator.LessThanOrEqualTo;

                topicInfo.ExtraConditions.Add(startCondition);
                topicInfo.ExtraConditions.Add(endCondition);

                response.RemoveNote(note);
            }
        }
    }

    private static ConditionFloat GetGlobalValueCondition(int hour, int minutes) {
        var data = new GetGlobalValueConditionData {
            RunOnType = Condition.RunOnType.Subject,
        };
        data.Global.Link.SetTo(Skyrim.Global.GameHour.FormKey);

        return new ConditionFloat {
            Data = data,
            ComparisonValue = hour + minutes / 60f,
        };
    }
}
