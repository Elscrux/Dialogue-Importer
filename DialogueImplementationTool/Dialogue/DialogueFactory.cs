using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class DialogueFactory(IDialogueContext context) : BaseDialogueFactory(context) {
    private readonly Dictionary<string, int> _npcIndices = new();

    public override void PreProcess(List<DialogueTopic> topics) { }

    public override void GenerateDialogue(List<DialogueTopic> topics) {
        foreach (var topic in topics) {
            // Use the first speaker for the editor id
            var speakerName = topic.TopicInfos[0].Speaker.Name;
            if (!_npcIndices.TryAdd(speakerName, 1)) _npcIndices[speakerName] += 1;

            var branch = new DialogBranch(Context.GetNextFormKey(), Context.Release) {
                EditorID = Context.Quest.EditorID + speakerName + _npcIndices[speakerName],
                Quest = new FormLinkNullable<IQuestGetter>(Context.Quest.FormKey),
            };

            branch.Flags |= topic.Blocking ? DialogBranch.Flag.Blocking : DialogBranch.Flag.TopLevel;
            Context.AddDialogBranch(branch);

            var startingFormKey = Context.GetNextFormKey();
            branch.StartingTopic = new FormLinkNullable<IDialogTopicGetter>(startingFormKey);

            var createdTopics = new List<LinkedTopic>();
            var topicQueue = new Queue<LinkedTopic>();
            topicQueue.Enqueue(new LinkedTopic(startingFormKey, topic, string.Empty, true));

            while (topicQueue.Any()) {
                var rawTopic = topicQueue.Dequeue();

                var playerText = rawTopic.Topic.GetPlayerText();
                var responses = GetTopicInfos(Context.Quest, rawTopic.Topic);
                var dontUsePrompt = !playerText.IsNullOrWhitespace();
                if (dontUsePrompt) {
                    foreach (var response in responses) {
                        response.Prompt = null;
                    }
                }

                var dialogTopic = new DialogTopic(rawTopic.FormKey, Context.Release) {
                    EditorID = $"{Context.Quest.EditorID}{speakerName}{_npcIndices[speakerName]}Topic{rawTopic.IndexString}",
                    Priority = 2500,
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
                            var newLink = new LinkedTopic(linkFormKey,
                                topicInfo.Links[linkIndex],
                                GetIndex(linkIndex + 1, !rawTopic.IndexType),
                                !rawTopic.IndexType);

                            createdTopics.Add(newLink);
                            topicQueue.Enqueue(newLink);

                            responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogGetter>(linkFormKey));
                        } else {
                            responses[topicInfoIndex].LinkTo.Add(new FormLink<IDialogGetter>(linkedTopic.FormKey));
                        }
                    }

                    string GetIndex(int index, bool type) {
                        if (topicInfo.InvisibleContinue) {
                            return rawTopic.IndexString + '_';
                        }

                        var nextChar = type
                            ? (char) (48 + index)
                            : (char) (64 + index);

                        return rawTopic.IndexString.TrimEnd('_') + nextChar;
                    }
                }
            }
        }
    }

    private sealed record LinkedTopic(FormKey FormKey, DialogueTopic Topic, string IndexString, bool IndexType);
}
