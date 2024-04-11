using System;
using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Model;

public sealed class SharedInfo {
    // todo remove static member
    private static readonly Dictionary<FormKey, int> SharedLineCount = new();
    private DialogTopic? _topic;

    public SharedInfo(DialogueTopicInfo responseDataTopicInfo) {
        if (responseDataTopicInfo.Speaker is null)
            throw new ArgumentException($"{nameof(responseDataTopicInfo)} can't have an empty speaker");

        ResponseDataTopicInfo = responseDataTopicInfo;
    }

    public DialogResponses? ResponseData { get; set; }
    public DialogueTopicInfo ResponseDataTopicInfo { get; }

    public DialogResponses GetResponseData(
        IQuest quest,
        IDialogueContext modContext,
        Func<IQuest, DialogueTopicInfo, FormKey?, DialogResponses> getResponses,
        Func<Speaker.ISpeaker, ExtendedList<Condition>> getSpeakerConditions) {
        if (ResponseData is null) {
            _topic ??= new DialogTopic(modContext.GetNextFormKey(), modContext.Release) {
                EditorID = $"{quest.EditorID}SharedInfos",
                Name = $"{quest.EditorID}SharedInfos",
                Priority = 2500,
                Quest = new FormLinkNullable<IQuestGetter>(quest.FormKey),
                Category = DialogTopic.CategoryEnum.Misc,
                Subtype = DialogTopic.SubtypeEnum.SharedInfo,
                SubtypeName = "IDAT",
                Responses = [],
            };

            modContext.AddDialogTopic(_topic);

            var lastFormKey = _topic.Responses.Count > 0 ? _topic.Responses[^1].FormKey : FormKey.Null;
            var dialogResponses = getResponses(quest, ResponseDataTopicInfo, lastFormKey);
            // Todo refactor with Naming.GetFirstFreeIndex
            dialogResponses.EditorID = GetNextSharedEditorID(quest.EditorID);

            _topic.Responses.Add(dialogResponses);

            ResponseData = dialogResponses;
        }

        return new DialogResponses(modContext.GetNextFormKey(), modContext.Release) {
            ResponseData = new FormLinkNullable<IDialogResponsesGetter>(ResponseData.FormKey),
            Conditions = getSpeakerConditions(ResponseDataTopicInfo.Speaker),
            FavorLevel = FavorLevel.None,
            Flags = new DialogResponseFlags(),
        };
    }

    private string GetNextSharedEditorID(string? questEditorId) {
        var newCount = 1;
        if (!SharedLineCount.TryGetValue(ResponseDataTopicInfo.Speaker.FormKey, out var count)) {
            SharedLineCount.Add(ResponseDataTopicInfo.Speaker.FormKey, newCount);
        } else {
            newCount = count + 1;
            SharedLineCount[ResponseDataTopicInfo.Speaker.FormKey] = newCount;
        }

        return questEditorId
               + ResponseDataTopicInfo.Speaker.Name
               + "Shared"
               + newCount;
    }
}
