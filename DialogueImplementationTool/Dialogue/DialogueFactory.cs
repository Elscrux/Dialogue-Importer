using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class DialogueFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    public override void PreProcess(List<DialogueTopic> topics) { }

    public override void GenerateDialogue(List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            // Use the first speaker for the editor id
            var speakerName = topic.TopicInfos[0].Speaker.Name;
            var baseName = Context.Quest.EditorID + speakerName;
            var branchEditorId = Naming.GetFirstFreeIndex(
                i => baseName + i,
                editorId => !Context.LinkCache.TryResolveIdentifier<IDialogBranchGetter>(editorId, out _),
                1);

            var branch = new DialogBranch(Context.GetNextFormKey(), Context.Release) { EditorID = branchEditorId, Quest = new FormLinkNullable<IQuestGetter>(Context.Quest.FormKey), };

            branch.Flags |= topic.Blocking ? DialogBranch.Flag.Blocking : DialogBranch.Flag.TopLevel;
            Context.AddDialogBranch(branch);

            var startingFormKey = Context.GetNextFormKey();
            branch.StartingTopic = new FormLinkNullable<IDialogTopicGetter>(startingFormKey);

            var createdTopics = new List<LinkedTopic>();
            var topicQueue = new Queue<LinkedTopic>();
            topicQueue.Enqueue(new LinkedTopic(startingFormKey, topic, string.Empty));

            while (topicQueue.Count != 0) {
                var rawTopic = topicQueue.Dequeue();

                var responses = GetTopicInfos(Context.Quest, rawTopic.Topic);
                var playerText = rawTopic.Topic.GetPlayerText();
                var dontUsePrompt = !playerText.IsNullOrWhitespace();
                if (dontUsePrompt) {
                    foreach (var response in responses) {
                        response.Prompt = null;
                    }
                }

                var editorId = $"{branchEditorId}Topic{rawTopic.Identifier}";

                var dialogTopic = new DialogTopic(rawTopic.FormKey, Context.Release) {
                    EditorID = editorId,
                    Priority = 50,
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
                        var linkedTopic = createdTopics.Find(t => t.Topic == topicInfo.Links[linkIndex]);
                        if (linkedTopic is null) {
                            var linkFormKey = Context.GetNextFormKey();
                            var newLink = new LinkedTopic(
                                linkFormKey,
                                topicInfo.Links[linkIndex],
                                GetIndex(linkIndex + 1));

                            createdTopics.Add(newLink);
                            topicQueue.Enqueue(newLink);

                            responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogGetter>(linkFormKey));
                        } else {
                            responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogGetter>(linkedTopic.FormKey));
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
    }

    private sealed record LinkedTopic(FormKey FormKey, DialogueTopic Topic, string Identifier);
}
