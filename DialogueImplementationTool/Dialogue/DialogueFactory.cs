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

                var implementedDialogueTopicGetter = Context.GetTopic(rawTopic.Topic);
                if (implementedDialogueTopicGetter is null) {
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
                } else {
                    if (editorId.Length < implementedDialogueTopicGetter.EditorID?.Length) {
                        var implementedTopicSetter = Context.GetTopic(implementedDialogueTopicGetter.FormKey);
                        implementedTopicSetter.EditorID = editorId;
                        implementedTopicSetter.Branch.SetTo(branch.FormKey);
                    }
                }

                // Add links
                for (var topicInfoIndex = 0; topicInfoIndex < rawTopic.Topic.TopicInfos.Count; topicInfoIndex++) {
                    var topicInfo = rawTopic.Topic.TopicInfos[topicInfoIndex];
                    for (var linkIndex = 0; linkIndex < topicInfo.Links.Count; linkIndex++) {
                        var nextIdentifier = GetIndex(topicInfo, rawTopic.Identifier, linkIndex + 1);

                        var currentLink = topicInfo.Links[linkIndex];
                        var implementedLinkedTopic = Context.GetTopic(currentLink);
                        if (implementedLinkedTopic is null) {
                            Console.WriteLine($"{nextIdentifier} {currentLink}: not implemented yet");
                            // Topic not implemented yet
                            var linkedTopic = topicQueue
                                .FirstOrDefault(x => ReferenceEquals(x.Topic, currentLink));

                            if (linkedTopic is null) {
                                Console.WriteLine($"{nextIdentifier} {currentLink}: Queueing topic");
                                // Queue up the linked topic for implementation
                                linkedTopic = new LinkedTopic(
                                    Context.GetNextFormKey(),
                                    currentLink,
                                    nextIdentifier);

                                topicQueue.Enqueue(linkedTopic);
                            } else {
                                Console.WriteLine($"{nextIdentifier} {currentLink}: Check existing queued topic");
                                // Use existing queued linked topic
                                // In case our identifier is shorter than the existing one,
                                // we need update it to keep the tree structure flat
                                if (nextIdentifier.Length < linkedTopic.Identifier.Length) {
                                    Console.WriteLine($"{nextIdentifier} {currentLink}: Updating identifier from {linkedTopic.Identifier} to {nextIdentifier}");
                                    linkedTopic.Identifier = nextIdentifier;
                                }
                            }

                            responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogGetter>(linkedTopic.FormKey));
                        } else {
                            // Use existing implemented topic
                            Console.WriteLine($"{nextIdentifier} {currentLink}: Using existing topic");
                            responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogGetter>(implementedLinkedTopic.FormKey));

                            // In case our identifier is shorter than the existing one,
                            // we need update it to keep the tree structure flat
                            var topicEditorID = GetTopicEditorID(branchEditorId, nextIdentifier);
                            if (topicEditorID.Length < implementedLinkedTopic.EditorID?.Length) {
                                Console.WriteLine($"{nextIdentifier} {currentLink}: Updating editor id from {implementedLinkedTopic.EditorID} to {topicEditorID}");
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
