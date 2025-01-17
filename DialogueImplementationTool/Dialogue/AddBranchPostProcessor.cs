using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class AddBranchPostProcessor(IDialogueContext context, DialogBranch.Flag flags) : IGenericDialoguePostProcessor {
    public void Process(Quest quest, DialogTopic dialogTopic) {
        var branchEditorId = dialogTopic.EditorID?.TrimEnd("Topic") + "Branch";
        context.GetOrAddRecord<DialogBranch, IDialogBranchGetter>(
            branchEditorId,
            () => new DialogBranch(context.GetNextFormKey(), context.Release) {
                EditorID = branchEditorId,
                Quest = quest.ToLink(),
                Flags = flags,
                StartingTopic = dialogTopic.ToNullableLink(),
            }
        );
    }
}
