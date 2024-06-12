using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class OffersServicesChecker : IDialogueTopicInfoProcessor {
    [GeneratedRegex("(?:if|when|is) (?:at|in|inside) (?:.* )?(?:store|stall)")]
    private static partial Regex OffersServicesRegex();

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            foreach (var note in response.Notes()) {
                var match = OffersServicesRegex().Match(note.Text);
                if (!match.Success) continue;

                topicInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = new GetOffersServicesNowConditionData(),
                    CompareOperator = CompareOperator.EqualTo,
                    ComparisonValue = 1,
                });

                response.RemoveNote(note);
            }
        }
    }
}
