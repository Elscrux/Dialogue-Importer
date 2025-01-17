using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class WIRemoveItemReturnPostProcessor(IDialogueContext context) : IGenericDialoguePostProcessor {
    public void Process(Quest quest, DialogTopic dialogTopic) {
        var forceGreet = context.LinkCache.Resolve(quest.Aliases[1].PackageData[1]);
        if (forceGreet.Data[7] is PackageDataTopic packageDataTopic) {
            packageDataTopic.Topics[0] = new TopicReference { Reference = dialogTopic.ToLink() };
        }

        dialogTopic.EditorID = quest.EditorID + "Topic";

        var addBranch = new AddBranchPostProcessor(context, DialogBranch.Flag.Blocking);
        addBranch.Process(quest, dialogTopic);
    }
}
