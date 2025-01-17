using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class WIRemoveItemReturnPostProcessor(IDialogueContext context) : IGenericDialoguePostProcessor {
    public void Process(Quest quest, DialogTopic dialogTopic) {
        var forceGreet = context.LinkCache.Resolve(quest.Aliases[1].PackageData[1]);
        if (forceGreet.Data[7] is PackageDataTopic packageDataTopic) {
            packageDataTopic.Topics[0] = new TopicReference { Reference = dialogTopic.ToLink() };
        }

        dialogTopic.EditorID = context.Prefix + "WIRemoveItemReturn";

        var branchEditorId = context.Prefix + "WIRemoveItemReturnBranch";
        var branch = context.GetOrAddRecord<DialogBranch, IDialogBranchGetter>(
            branchEditorId,
            () => new DialogBranch(context.GetNextFormKey(), context.Release) {
                EditorID = branchEditorId,
                Quest = quest.ToLink(),
                Flags = DialogBranch.Flag.Blocking,
                StartingTopic = dialogTopic.ToNullableLink(),
            }
        );

        context.AddRecord(branch);
    }
}
