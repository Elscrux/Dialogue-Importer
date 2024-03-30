using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class Dialogue : DialogueFactory {
	private static readonly Dictionary<string, int> NPCIndices = new();

	public override void PreProcess(List<DialogueTopic> topics) {}

	private sealed record LinkedTopic(FormKey FormKey, DialogueTopic Topic, string IndexString, bool IndexType);

	public override void GenerateDialogue(List<DialogueTopic> topics) {
		foreach (var dialogueTopic in topics) {
			if (NPCIndices.ContainsKey(dialogueTopic.Speaker.Name)) {
				NPCIndices[dialogueTopic.Speaker.Name] += 1;
			} else {
				NPCIndices.Add(dialogueTopic.Speaker.Name, 1);
			}

			var branch = new DialogBranch(Mod.GetNextFormKey(), Release) {
				EditorID = DialogueImplementer.Quest.EditorID + dialogueTopic.Speaker.Name + NPCIndices[dialogueTopic.Speaker.Name],
				Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
			};

			branch.Flags |= dialogueTopic.Blocking
				? DialogBranch.Flag.Blocking
				: DialogBranch.Flag.TopLevel;
			Mod.DialogBranches.Add(branch);

			var startingFormKey = Mod.GetNextFormKey();
			branch.StartingTopic = new FormLinkNullable<IDialogTopicGetter>(startingFormKey);

			var createdTopics = new List<LinkedTopic>();
			var topicQueue = new Queue<LinkedTopic>();
			topicQueue.Enqueue(new LinkedTopic(startingFormKey, dialogueTopic, string.Empty, true));

			while (topicQueue.Any()) {
				var rawTopic = topicQueue.Dequeue();

				var responses = GetResponsesList(rawTopic.Topic);
				var dialogTopic = new DialogTopic(rawTopic.FormKey, Release) {
					EditorID = $"{DialogueImplementer.Quest.EditorID}{dialogueTopic.Speaker.Name}{NPCIndices[dialogueTopic.Speaker.Name]}Topic{rawTopic.IndexString}",
					Priority = 2500,
					Name = rawTopic.Topic.Text,
					Branch = new FormLinkNullable<IDialogBranchGetter>(branch),
					Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
					Subtype = DialogTopic.SubtypeEnum.Custom,
					Category = DialogTopic.CategoryEnum.Topic,
					SubtypeName = "CUST",
					Responses = responses,
				};
				Mod.DialogTopics.Add(dialogTopic);

				for (var i = 0; i < rawTopic.Topic.Links.Count; i++) {
					var linkedTopic = createdTopics.Find(t => t.Topic == rawTopic.Topic.Links[i]);
					if (linkedTopic == null) {
						var linkFormKey = Mod.GetNextFormKey();
						var newLink = new LinkedTopic(linkFormKey, rawTopic.Topic.Links[i], rawTopic.IndexString + GetIndex(i + 1, !rawTopic.IndexType), !rawTopic.IndexType);

						createdTopics.Add(newLink);
						topicQueue.Enqueue(newLink);

						responses[0].LinkTo.Add(new FormLink<IDialogGetter>(linkFormKey));
					} else {
						responses[0].LinkTo.Add(new FormLink<IDialogGetter>(linkedTopic.FormKey));
					}
				}
			}

			char GetIndex(int index, bool type) => type ? (char) (48 + index) : (char) (64 + index);
		}
	}

	public override void PostProcess() {}
}
