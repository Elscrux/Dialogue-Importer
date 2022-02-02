using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue; 

public abstract class OneLinerFactory : DialogueFactory {
    protected static void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, DialogTopic dialogTopic) {
        var lastFormKey = FormKey.Null;
        if (dialogTopic.Responses.Count > 0) {
            lastFormKey = dialogTopic.Responses[^1].FormKey;
        }

        for (var i = 0; i < topics[0].Responses.Count; i++) {
            var response = topics[0].Responses[i];

            var dialogResponses = new DialogResponses(Mod.GetNextFormKey(), Release) {
                Conditions = new ExtendedList<Condition> { GetIsIDCondition(speakerKey) },
                FavorLevel = FavorLevel.None,
                Responses = new ExtendedList<DialogResponse> {
                    new() {
                        Text = response,
                        ResponseNumber = (byte) i,
                        EmotionValue = 50
                    }
                },
                Flags = new DialogResponseFlags(),
                PreviousDialog = new FormLinkNullable<IDialogResponsesGetter>(lastFormKey)
            };
            lastFormKey = dialogResponses.FormKey;

            dialogTopic.Responses.Add(dialogResponses);
        }

        if (!Mod.DialogTopics.ContainsKey(dialogTopic.FormKey)) {
            Mod.DialogTopics.Add(dialogTopic);
        }
    }
}