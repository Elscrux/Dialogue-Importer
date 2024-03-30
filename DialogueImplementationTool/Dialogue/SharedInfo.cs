using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Topics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class SharedInfo {
	private static DialogTopic? _topic;
	private static readonly Dictionary<FormKey, int> SharedLineCount = new();

	public DialogResponses? ResponseData { get; set; }
	public DialogueTopic ResponseDataTopic { get; }

	public SharedInfo(DialogueTopic responseDataTopic) {
		if (responseDataTopic.Speaker == null) throw new ArgumentException($"{nameof(responseDataTopic)} can't have an empty speaker");

		ResponseDataTopic = responseDataTopic;
	}

	public DialogResponses GetResponseData() {
		if (ResponseData == null) {
			_topic ??= new DialogTopic(DialogueFactory.Mod.GetNextFormKey(), DialogueFactory.Release) {
				EditorID = $"{DialogueImplementer.Quest.EditorID}SharedInfos",
				Name = $"{DialogueImplementer.Quest.EditorID}SharedInfos",
				Priority = 2500,
				Quest = new FormLinkNullable<IQuestGetter>(DialogueImplementer.Quest.FormKey),
				Category = DialogTopic.CategoryEnum.Misc,
				Subtype = DialogTopic.SubtypeEnum.SharedInfo,
				SubtypeName = "IDAT",
				Responses = new ExtendedList<DialogResponses>(),
			};

			if (!DialogueFactory.Mod.DialogTopics.ContainsKey(_topic.FormKey)) {
				DialogueFactory.Mod.DialogTopics.Add(_topic);
			}

			var lastFormKey = _topic.Responses.Count > 0 ? _topic.Responses[^1].FormKey : FormKey.Null;
			var dialogResponses = DialogueFactory.GetResponses(ResponseDataTopic, lastFormKey);
			dialogResponses.EditorID = GetNextSharedEditorID();

			_topic.Responses.Add(dialogResponses);

			ResponseData = dialogResponses;
		}

		return new DialogResponses(DialogueFactory.Mod.GetNextFormKey(), DialogueFactory.Release) {
			ResponseData = new FormLinkNullable<IDialogResponsesGetter>(ResponseData.FormKey),
			Conditions = DialogueFactory.GetSpeakerConditions(ResponseDataTopic.Speaker),
			FavorLevel = FavorLevel.None,
			Flags = new DialogResponseFlags(),
		};
	}

	private string GetNextSharedEditorID() {
		var newCount = 1;
		if (!SharedLineCount.TryGetValue(ResponseDataTopic.Speaker.FormKey, out var count)) {
			SharedLineCount.Add(ResponseDataTopic.Speaker.FormKey, newCount);
		} else {
			newCount = count + 1;
			SharedLineCount[ResponseDataTopic.Speaker.FormKey] = newCount;
		}

		return DialogueImplementer.Quest.EditorID
		  + ResponseDataTopic.Speaker.Name
		  + "Shared"
		  + newCount;
	}
}
