using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class WIRemoveItemReturnPostProcessor(IDialogueContext context) : IGenericDialoguePostProcessor {
    public void Process(Quest quest, DialogTopic dialogTopic) {
        var forceGreet = context.LinkCache.Resolve(quest.Aliases[1].PackageData[1]);
        if (forceGreet.Data[7] is PackageDataTopic packageDataTopic) {
            packageDataTopic.Topics[0] = new TopicReference { Reference = dialogTopic.ToLink() };
        }

        var branch = new DialogBranch(context.GetNextFormKey(), context.Release) {
            EditorID = context.Prefix + "WIRemoveItemReturnBranch",
            Quest = quest.ToLink(),
            Flags = DialogBranch.Flag.Blocking,
            StartingTopic = dialogTopic.ToNullableLink(),
        };

        context.AddRecord(branch);
    }
}
