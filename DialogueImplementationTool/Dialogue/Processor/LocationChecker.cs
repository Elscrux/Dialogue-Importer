using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class LocationChecker(IDialogueContext context) : IDialogueTopicInfoProcessor {
    [GeneratedRegex("^Location: (.+)", RegexOptions.IgnoreCase)]
    private static partial Regex LocationRegex { get; }

    public void Process(DialogueTopicInfo topicInfo) {
        topicInfo.Prompt.StartNotes.RemoveAll(CheckNote);
        topicInfo.Prompt.EndsNotes.RemoveAll(CheckNote);
        foreach (var response in topicInfo.Responses) {
            response.StartNotes.RemoveAll(CheckNote);
            response.EndsNotes.RemoveAll(CheckNote);
        }

        bool CheckNote(Note note) {
            var match = LocationRegex.Match(note.Text);
            if (!match.Success) return false;

            var questName = match.Groups[1].Value;
            var record = context.SelectRecordCanBeNull(
                $"Matching location/location keyword/region/worldspace/formlist of worldspaces for {questName}",
                typeof(ILocationGetter),
                typeof(IKeywordGetter),
                typeof(IRegionGetter),
                typeof(IWorldspaceGetter),
                typeof(IFormListGetter));
            if  (record is null) return false;

            switch (record) {
                case ILocationGetter location:
                    topicInfo.ExtraConditions.Add(new GetInCurrentLocConditionData {
                        Location = { Link = { FormKey = location.FormKey } },
                    }.ToConditionFloat());
                    break;
                case IKeywordGetter keyword:
                    topicInfo.ExtraConditions.Add(new LocationHasKeywordConditionData {
                        Keyword = { Link = { FormKey = keyword.FormKey } },
                    }.ToConditionFloat());
                    break;
                case IRegionGetter region:
                    topicInfo.ExtraConditions.Add(new IsPlayerInRegionConditionData {
                        Region = { Link = { FormKey = region.FormKey } },
                    }.ToConditionFloat());
                    break;
                case IWorldspaceGetter worldspace:
                    topicInfo.ExtraConditions.Add(new GetInWorldspaceConditionData {
                        WorldspaceOrList = { Link = { FormKey = worldspace.FormKey } },
                    }.ToConditionFloat());
                    break;
                case IFormListGetter formList:
                    topicInfo.ExtraConditions.Add(new GetInWorldspaceConditionData {
                        WorldspaceOrList = { Link = { FormKey = formList.FormKey } },
                    }.ToConditionFloat());
                    break;
            }

            return true;
        }
    }
}
