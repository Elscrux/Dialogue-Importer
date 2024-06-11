using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class TalkedToChecker(IDialogueContext context) : IDialogueTopicInfoProcessor {
    [GeneratedRegex("(?:PC|player )?.*(?:met|spoken|talked) (?:to )?(.+)")]
    private static partial Regex TalkedToRegex();

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            foreach (var note in response.Notes()) {
                var match = TalkedToRegex().Match(note.Text);
                if (!match.Success) continue;

                var npc = context.SelectRecord<Npc, INpcGetter>(match.Groups[1].Value);
                var placedNpc = context.LinkCache.PriorityOrder.WinningOverrides<IPlacedNpcGetter>()
                    .FirstOrDefault(placedNpc => placedNpc.Base.FormKey == npc.FormKey);
                if (placedNpc == null) continue;

                var data = new GetTalkedToPCParamConditionData();
                data.TargetNpc.Link.SetTo(placedNpc.FormKey);
                topicInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = data,
                    CompareOperator = CompareOperator.EqualTo,
                    ComparisonValue = 1,
                });

                response.RemoveNote(note);
            }
        }
    }
}
