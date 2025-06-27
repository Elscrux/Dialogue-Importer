using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class TalkedToChecker(IDialogueContext context) : IDialogueTopicInfoProcessor {
    [GeneratedRegex(@"(?:PC|player )?\b(?:met|spoken|talked)\b(?:to )?(.+)")]
    private static partial Regex TalkedToRegex { get; }

    public void Process(DialogueTopicInfo topicInfo) {
        foreach (var response in topicInfo.Responses) {
            foreach (var note in response.Notes()) {
                var match = TalkedToRegex.Match(note.Text);
                if (!match.Success) continue;

                var npc = context.SelectRecord<Npc, INpcGetter>(match.Groups[1].Value);
                var placedNpc = context.LinkCache.PriorityOrder.WinningOverrides<IPlacedNpcGetter>()
                    .FirstOrDefault(placedNpc => placedNpc.Base.FormKey == npc.FormKey);
                if (placedNpc == null) continue;

                topicInfo.ExtraConditions.Add(new ConditionFloat {
                    Data = new GetTalkedToPCParamConditionData { TargetNpc = { Link = { FormKey = placedNpc.FormKey } } },
                    CompareOperator = CompareOperator.EqualTo,
                    ComparisonValue = 1,
                });

                response.RemoveNote(note);
            }
        }
    }
}
