using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue;

public sealed class Idle : OneLinerFactory {
	private static readonly PostProcessOptions PostProcessOptions = new(true);
	private static DialogTopic? _topic;

	public override void GenerateDialogue(List<DialogueTopic> topics) {
		_topic ??= new DialogTopic(Mod.GetNextFormKey(), Release) {
			EditorID = $"{DialogueImplementer.Quest.EditorID}Idles",
			Name = $"{DialogueImplementer.Quest.EditorID}Idles",
			Priority = 2500,
			Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
			Category = DialogTopic.CategoryEnum.Misc,
			Subtype = DialogTopic.SubtypeEnum.Idle,
			SubtypeName = "IDLE"
		};

		GenerateDialogue(topics, _topic);
	}

	public override void PostProcess() {
		if (_topic != null) PostProcess(_topic, PostProcessOptions);
	}
}
