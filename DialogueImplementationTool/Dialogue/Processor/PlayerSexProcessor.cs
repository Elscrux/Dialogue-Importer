using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class PlayerSexProcessor : IGenericDialogueProcessor {
    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        if (genericDialogue.PlayerSex is null) return;

        var condition = GetCondition(genericDialogue.PlayerSex.Value);
        topicInfo.ExtraConditions.Add(condition);
    }

    private static Condition GetCondition(MaleFemaleGender maleFemaleGender) {
        return new GetPCIsSexConditionData {
            MaleFemaleGender = maleFemaleGender,
        }.ToConditionFloat();
    }
}
