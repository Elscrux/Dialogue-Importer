using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class DialogueFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    public override void PreProcess(List<DialogueTopic> topics) {}

    public override void GenerateDialogue(List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            // Use the first speaker for the editor id
            var speakerName = topic.TopicInfos[0].Speaker.NameNoSpaces;
            var baseName = Context.Quest.EditorID + speakerName;
            var branchEditorId = Naming.GetFirstFreeIndex(
                i => baseName + i,
                editorId => !Context.LinkCache.TryResolveIdentifier<IDialogBranchGetter>(editorId, out _),
                1);

            var branch = new DialogBranch(Context.GetNextFormKey(), Context.Release) {
                EditorID = branchEditorId,
                Quest = new FormLinkNullable<IQuestGetter>(Context.Quest.FormKey),
            };

            branch.Flags ??= new DialogBranch.Flag();
            branch.Flags |= topic.Blocking ? DialogBranch.Flag.Blocking : DialogBranch.Flag.TopLevel;
            Context.AddDialogBranch(branch);

            var startingFormKey = Context.GetNextFormKey();
            branch.StartingTopic = new FormLinkNullable<IDialogTopicGetter>(startingFormKey);

            var topicQueue = new Queue<LinkedTopic>();
            topicQueue.Enqueue(new LinkedTopic(startingFormKey, topic, string.Empty));

            while (topicQueue.Count != 0) {
                var rawTopic = topicQueue.Dequeue();

                var responses = GetTopicInfos(Context.Quest, rawTopic.Topic);
                var playerText = rawTopic.Topic.GetPlayerFullText();
                var dontUsePrompt = !playerText.IsNullOrWhitespace();
                if (dontUsePrompt) {
                    foreach (var response in responses) {
                        response.Prompt = null;
                    }
                }

                var editorId = GetTopicEditorID(branchEditorId, rawTopic.Identifier);

                var dialogTopic = new DialogTopic(rawTopic.FormKey, Context.Release) {
                    EditorID = editorId,
                    Priority = GetDialoguePriority(Context.Quest, int.Parse(branchEditorId[baseName.Length..])),
                    Name = dontUsePrompt ? playerText : null,
                    Branch = new FormLinkNullable<IDialogBranchGetter>(branch),
                    Quest = new FormLinkNullable<IQuestGetter>(Context.Quest.FormKey),
                    Subtype = DialogTopic.SubtypeEnum.Custom,
                    Category = DialogTopic.CategoryEnum.Topic,
                    SubtypeName = "CUST",
                    Responses = responses,
                };
                Context.AddDialogTopic(dialogTopic);

                // Add links
                for (var topicInfoIndex = 0; topicInfoIndex < rawTopic.Topic.TopicInfos.Count; topicInfoIndex++) {
                    var topicInfo = rawTopic.Topic.TopicInfos[topicInfoIndex];
                    for (var linkIndex = 0; linkIndex < topicInfo.Links.Count; linkIndex++) {
                        var nextIdentifier = GetIndex(linkIndex + 1);

                        var implementedLinkedTopic = Context.GetTopic(topicInfo.Links[linkIndex]);
                        if (implementedLinkedTopic is null) {
                            // Topic not implemented yet
                            var linkedTopic = topicQueue
                                .FirstOrDefault(x => x.Topic == topicInfo.Links[linkIndex]);

                            if (linkedTopic is null) {
                                // Queue up the linked topic for implementation
                                linkedTopic = new LinkedTopic(
                                    Context.GetNextFormKey(),
                                    topicInfo.Links[linkIndex],
                                    nextIdentifier);

                                topicQueue.Enqueue(linkedTopic);
                            } else {
                                // Use existing queued linked topic
                                // In case our identifier is shorter than the existing one,
                                // we need update it to keep the tree structure flat
                                if (nextIdentifier.Length < linkedTopic.Identifier.Length) {
                                    linkedTopic.Identifier = nextIdentifier;
                                }
                            }

                            responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogGetter>(linkedTopic.FormKey));
                        } else {
                            // Use existing implemented topic
                            responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogGetter>(implementedLinkedTopic.FormKey));

                            // In case our identifier is shorter than the existing one,
                            // we need update it to keep the tree structure flat
                            var topicEditorID = GetTopicEditorID(branchEditorId, nextIdentifier);
                            if (topicEditorID.Length < implementedLinkedTopic.EditorID?.Length) {
                                var implementedTopic = Context.GetTopic(implementedLinkedTopic.FormKey);
                                implementedTopic.EditorID = topicEditorID;
                                implementedTopic.Branch.SetTo(branch.FormKey);
                            }
                        }
                    }

                    string GetIndex(int index) {
                        // Invisible continues add an underscore to the identifier
                        if (topicInfo.InvisibleContinue) {
                            return rawTopic.Identifier + '_';
                        }

                        // Non-invisible continues don't have an underscore 
                        var lastIdentifier = rawTopic.Identifier.TrimEnd('_');

                        // If this is the first link, return the first identifier
                        if (lastIdentifier.Length == 0) {
                            return LetterChar().ToString();
                        }

                        // Alternate between letters and numbers
                        var lastChar = lastIdentifier.Last();
                        if (char.IsLetter(lastChar)) {
                            return lastIdentifier + NumberChar();
                        }

                        return lastIdentifier + LetterChar();

                        char NumberChar() => (char) (48 + index);
                        char LetterChar() => (char) (64 + index);
                    }
                }
            }
        }

        string GetTopicEditorID(string branchEditorId, string identifier) {
            return $"{branchEditorId}Topic{identifier}";
        }
    }

    private static float GetDialoguePriority(IQuest quest, int dialogueIndex) {
        return quest.Type switch {
            Quest.TypeEnum.Misc => 70,
            Quest.TypeEnum.Daedric => 85,
            Quest.TypeEnum.SideQuest => 85,
            _ => quest.IsDialogueQuest()
                ? Math.Clamp(50 - (dialogueIndex - 1) * 5, 0, 50)
                : 50
        };
    }

    private sealed record LinkedTopic(FormKey FormKey, DialogueTopic Topic, string Identifier) {
        public string Identifier { get; set; } = Identifier;
    }
}
