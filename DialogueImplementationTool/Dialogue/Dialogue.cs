using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public class Dialogue : DialogueFactory {
    private readonly Dictionary<string, int> _npcIndices = new();

    public override void GenerateDialogue(List<DialogueTopic> topics, FormKey speakerKey, string speakerName) {
        foreach (var dialogueTopic in topics) {
            if (_npcIndices.ContainsKey(speakerName)) {
                _npcIndices[speakerName] += 1;
            } else {
                _npcIndices.Add(speakerName, 1);
            }
            
            var branch = new DialogBranch(Mod.GetNextFormKey(), Release) {
                EditorID = DialogueImplementer.Quest.EditorID + speakerName + _npcIndices[speakerName],
                Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
                Flags = DialogBranch.Flag.TopLevel
            };
            Mod.DialogBranches.Add(branch);
            
            branch.StartingTopic = new FormLinkNullable<IDialogTopicGetter>(CreateTopic(dialogueTopic, string.Empty, true));

            DialogTopic CreateTopic(DialogueTopic rawTopic, string indexString, bool indexType) {
                indexType = !indexType;

                var responses = GetResponsesList(speakerKey, rawTopic);
                var dialogTopic = new DialogTopic(Mod.GetNextFormKey(), Release) {
                    EditorID = $"{DialogueImplementer.Quest.EditorID}{speakerName}{_npcIndices[speakerName]}Topic{indexString}",
                    Priority = 2500,
                    Name = rawTopic.Text,
                    Branch = new FormLinkNullable<IDialogBranchGetter>(branch),
                    Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
                    Subtype = DialogTopic.SubtypeEnum.Custom,
                    Category = DialogTopic.CategoryEnum.Topic,
                    SubtypeName = "CUST",
                    Responses = responses,
                };
                Mod.DialogTopics.Add(dialogTopic);

                for (var i = 0; i < rawTopic.Links.Count; i++) {
                    responses[0].LinkTo.Add(new FormLink<IDialogGetter>(
                        CreateTopic(rawTopic.Links[i], indexString + GetIndex(i + 1, indexType), indexType)
                    ));
                }

                return dialogTopic;
            }

            char GetIndex(int index, bool type) => type ? (char) (48 + index) : (char) (64 + index);
        }
    }
    
    public override void PostProcess() {}
}