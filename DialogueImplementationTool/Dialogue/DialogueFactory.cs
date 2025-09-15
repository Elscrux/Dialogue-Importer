using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class DialogueFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    public override IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) {
        dialogueProcessor.ConversationProcessors.Add(new BlockingChecker());
        return dialogueProcessor;
    }

    public override void GenerateDialogue(List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            if (topic.ServiceType == ServiceType.Default) {
                var (branch, branchIndex) = CreateBranch(topic);

                AddTopic(topic, branch, branchIndex);
            } else {
                var defaultBranchFormKey = topic.ServiceType switch {
                    ServiceType.Vendor => Skyrim.DialogBranch.ServicesBranch.FormKey,
                    ServiceType.Rumor => FormKey.Null,
                    ServiceType.RentRoom => Skyrim.DialogBranch.RentRoomBranch.FormKey,
                    ServiceType.Train => Skyrim.DialogBranch.TrainingBranch.FormKey,
                    ServiceType.Beggar => Skyrim.DialogBranch.FavorJobsBeggarsGiveMoney.FormKey,
                    _ => FormKey.Null
                };

                var branch = Context.GetServiceBranch(topic.ServiceType, defaultBranchFormKey);
                // Branch is null if this dialogue should be discarded and default dialogue should be used instead
                if (branch is null) continue;

                // Add responses to the end of the responses list
                var dialogTopic = Context.GetTopic(branch.StartingTopic.FormKey);
                var quest = Context.LinkCache.Resolve<IQuestGetter>(branch.Quest.FormKey);
                var responses = GetTopicInfos(quest, topic);
                if (dialogTopic.Responses.Count > 0 && responses.Count > 0) {
                    responses[0].PreviousDialog.SetTo(dialogTopic.Responses[^1].FormKey);
                }
                dialogTopic.Responses.AddRange(responses);
            }
        }
    }

    private void AddTopic(DialogueTopic topic, DialogBranch branch, int branchIndex = 1) {
        var branchEditorId = branch.EditorID;
        var startingFormKey = Context.GetNextFormKey();
        var quest = Context.LinkCache.Resolve<IQuestGetter>(branch.Quest.FormKey);

        var topicQueue = new Queue<LinkedTopic>();
        topicQueue.Enqueue(new LinkedTopic(startingFormKey, topic, string.Empty));

        while (topicQueue.Count != 0) {
            var rawTopic = topicQueue.Dequeue();

            var responses = GetTopicInfos(quest, rawTopic.Topic);
            var playerText = rawTopic.Topic.GetPlayerFullText();
            var dontUsePrompt = !playerText.IsNullOrWhitespace();
            if (dontUsePrompt) {
                foreach (var response in responses) {
                    response.Prompt = null;
                }
            }

            var editorId = GetTopicEditorID(rawTopic.Identifier);

            var implementedDialogueTopicGetter = Context.GetTopic(rawTopic.Topic);
            if (implementedDialogueTopicGetter is null) {
                var dialogTopic = new DialogTopic(rawTopic.FormKey, Context.Release) {
                    EditorID = editorId,
                    Name = dontUsePrompt ? playerText : null,
                    Branch = new FormLinkNullable<IDialogBranchGetter>(branch),
                    Quest = new FormLinkNullable<IQuestGetter>(branch.Quest.FormKey),
                    Subtype = DialogTopic.SubtypeEnum.Custom,
                    Category = DialogTopic.CategoryEnum.Topic,
                    SubtypeName = "CUST",
                    Responses = responses,
                };
                implementedDialogueTopicGetter = dialogTopic;
                Context.AddRecord(dialogTopic);

                // Set the starting topic
                if (rawTopic.Identifier == string.Empty) {
                    branch.StartingTopic = new FormLinkNullable<IDialogTopicGetter>(rawTopic.FormKey);
                    dialogTopic.Priority = GetDialoguePriority(quest, branchIndex);
                }
            } else {
                if (editorId.Length < implementedDialogueTopicGetter.EditorID?.Length) {
                    var implementedTopicSetter = Context.GetTopic(implementedDialogueTopicGetter.FormKey);
                    implementedTopicSetter.EditorID = editorId;
                    implementedTopicSetter.Branch.SetTo(branch.FormKey);
                }

                // Set the starting topic
                if (rawTopic.Identifier == string.Empty) {
                    branch.StartingTopic = new FormLinkNullable<IDialogTopicGetter>(implementedDialogueTopicGetter.FormKey);
                }
            }

            // Add links
            for (var topicInfoIndex = 0; topicInfoIndex < rawTopic.Topic.TopicInfos.Count; topicInfoIndex++) {
                var topicInfo = rawTopic.Topic.TopicInfos[topicInfoIndex];
                for (var linkIndex = 0; linkIndex < topicInfo.Links.Count; linkIndex++) {
                    var nextIdentifier = GetIndex(topicInfo, rawTopic.Identifier, linkIndex + 1);

                    var currentLink = topicInfo.Links[linkIndex];
                    var implementedLinkedTopic = currentLink.Equals(rawTopic.Topic)
                        ? implementedDialogueTopicGetter
                        :  Context.GetTopic(currentLink);
                    if (implementedLinkedTopic is null) {
                        // Topic not implemented yet
                        var linkedTopic = topicQueue.FirstOrDefault(x => currentLink.Equals(x.Topic));

                        if (linkedTopic is null) {
                            // Queue up the linked topic for implementation
                            linkedTopic = new LinkedTopic(
                                Context.GetNextFormKey(),
                                currentLink,
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

                        responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogTopicGetter>(linkedTopic.FormKey));
                    } else {
                        // Use existing implemented topic
                        responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogTopicGetter>(implementedLinkedTopic.FormKey));

                        // In case our identifier is shorter than the existing one,
                        // we need update it to keep the tree structure flat
                        var topicEditorID = GetTopicEditorID(nextIdentifier);
                        if (topicEditorID.Length < implementedLinkedTopic.EditorID?.Length) {
                            var implementedLinkedTopicSetter = Context.GetTopic(implementedLinkedTopic.FormKey);
                            implementedLinkedTopicSetter.EditorID = topicEditorID;
                            implementedLinkedTopicSetter.Branch.SetTo(branch.FormKey);

                            // Add all links again to ensure they have the proper naming
                            foreach (var info in currentLink.TopicInfos) {
                                for (var i = 0; i < info.Links.Count; i++) {
                                    var linkTopic = info.Links[i];
                                    var linkIdentifier = GetIndex(info, nextIdentifier, linkIndex + 1 + i);
                                    var linkedTopic = new LinkedTopic(
                                        Context.GetNextFormKey(),
                                        linkTopic,
                                        linkIdentifier);

                                    topicQueue.Enqueue(linkedTopic);
                                }
                            }
                        }
                    }
                }

                string GetIndex(DialogueTopicInfo info, string currentIdentifier, int index) {
                    // Invisible-continues add an underscore to the identifier
                    if (info.InvisibleContinue) {
                        return currentIdentifier + '_';
                    }

                    // Non-invisible continues don't have an underscore 
                    var lastIdentifier = currentIdentifier.TrimEnd('_');

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

        string GetTopicEditorID(string identifier) => $"{branchEditorId}Topic{identifier}";
    }

    private (DialogBranch branch, int branchIndex) CreateBranch(DialogueTopic topic) {
        // Use the first speaker for the editor id
        var speakerName = topic.TopicInfos[0].Speaker.NameNoSpaces;
        var quest = Context.LinkCache.Resolve<IQuestGetter>(Context.Quest.FormKey);
        var blockingSuffix = topic.Blocking && quest.IsDialogueQuest() ? "Blocking" : string.Empty;
        var baseName = Context.Quest.EditorID + speakerName + blockingSuffix;
        var branchEditorId = Naming.GetFirstFreeIndex(
            i => baseName + i,
            editorId => !Context.LinkCache.TryResolveIdentifier<IDialogBranchGetter>(editorId, out _),
            1);
        var branchIndex = int.Parse(branchEditorId[baseName.Length..]);

        var branch = new DialogBranch(Context.GetNextFormKey(), Context.Release) {
            EditorID = branchEditorId,
            Quest = new FormLinkNullable<IQuestGetter>(Context.Quest.FormKey),
        };

        branch.Flags ??= new DialogBranch.Flag();
        branch.Flags |= topic.Blocking ? DialogBranch.Flag.Blocking : DialogBranch.Flag.TopLevel;
        Context.AddRecord(branch);

        return (branch, branchIndex);
    }

    private static float GetDialoguePriority(IQuestGetter quest, int dialogueIndex) {
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
