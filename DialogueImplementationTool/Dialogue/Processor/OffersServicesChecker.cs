using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class OffersServicesChecker : IDialogueTopicInfoProcessor {
    [GeneratedRegex("(?:if|when|is) (?:at|in|inside) (?:.* )?(?:store|stall)", RegexOptions.IgnoreCase)]
    private static partial Regex OffersServicesRegex();

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            var notes = response.Notes()
                .Where(x => OffersServicesRegex().IsMatch(x.Text))
                .ToList();

            if (notes.Count == 0) continue;

            topicInfo.ExtraConditions.Add(new ConditionFloat {
                Data = new GetOffersServicesNowConditionData(),
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1,
            });

            foreach (var note in notes) {
                response.RemoveNote(note);
            }
        }
    }
}
