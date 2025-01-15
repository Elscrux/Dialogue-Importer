using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class GuardDialogueQuestFactory(IDialogueContext context) : IGenericDialogueQuestFactory {
    public Quest Create() {
        var questEditorId = context.Prefix + "GuardDialogue";
        var guardFaction = context.SelectRecord<Faction, IFactionGetter>("Guard Faction");
        var disableFaction = context.SelectRecord<Faction, IFactionGetter>("Guard Dialogue Disable Faction");

        return context.GetOrAddRecord<Quest, IQuestGetter>(
            questEditorId,
            () => new Quest(context.GetNextFormKey(), context.Release) {
                EditorID = questEditorId,
                Priority = 30,
                Filter = context.Quest.Filter,
                DialogConditions = [
                    new IsGuardConditionData().ToConditionFloat(),
                    new GetInFactionConditionData {
                        Faction = {Link = { FormKey = guardFaction.FormKey }}
                    }.ToConditionFloat(),
                    new GetInFactionConditionData {
                        Faction = {Link = { FormKey = disableFaction.FormKey }}
                    }.ToConditionFloat(comparisonValue: 0),
                ],
                Name = "Guard Dialogue",
                Flags = Quest.Flag.StartGameEnabled,
            });
    }
}
