using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Model;

public sealed class SharedInfo {
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
        Func<DialogueTopicInfo, IEnumerable<DialogResponse>> getResponses,
        Func<DialogueTopicInfo, ExtendedList<Condition>> getConditions) {
        if (ResponseData is null) {
            var dialogResponses = getResponses(ResponseDataTopicInfo);
            var dialogTopic = modContext.LinkCache.PriorityOrder
                .SelectMany(x => x.EnumerateMajorRecords<IDialogTopicGetter>())
                .FirstOrDefault(t => t is { Subtype: DialogTopic.SubtypeEnum.SharedInfo, IsDeleted: false } && t.Quest.FormKey == quest.FormKey);

            // Create new shared info topic if it doesn't exist in the current quest
            DialogTopic newDialogueTopic;
            if (dialogTopic is null) {
                var dialogTopicEditorId = Naming.GetFirstFreeIndex(
                    i => i == 1 ? $"{quest.EditorID}Shared" : $"{quest.EditorID}Shared{i}",
                    name => !modContext.LinkCache.TryResolve<IDialogResponsesGetter>(name, out _),
                    1);

                newDialogueTopic = new DialogTopic(modContext.GetNextFormKey(), modContext.Release) {
                    EditorID = dialogTopicEditorId,
                    Name = dialogTopicEditorId,
                    Priority = 50,
                    Quest = new FormLinkNullable<IQuestGetter>(quest.FormKey),
                    Category = DialogTopic.CategoryEnum.Misc,
                    Subtype = DialogTopic.SubtypeEnum.SharedInfo,
                    SubtypeName = "IDAT",
                    Responses = [],
                };
                modContext.AddDialogTopic(newDialogueTopic);
            } else {
                newDialogueTopic = modContext.GetTopic(dialogTopic.FormKey);
            }

            var responses = new DialogResponses(modContext.GetNextFormKey(), modContext.Release) {
                EditorID = Naming.GetFirstFreeIndex(
                    i => $"{quest.EditorID}{ResponseDataTopicInfo.Speaker.Name}Shared{i}",
                    name => !modContext.LinkCache.TryResolve<IDialogResponsesGetter>(name, out _),
                    1),
                Responses = dialogResponses.ToExtendedList(),
                Conditions = getConditions(ResponseDataTopicInfo),
            };
            newDialogueTopic.Responses.Add(responses);

            ResponseData = responses;
        }

        return new DialogResponses(modContext.GetNextFormKey(), modContext.Release) {
            ResponseData = new FormLinkNullable<IDialogResponsesGetter>(ResponseData.FormKey),
            Conditions = getConditions(ResponseDataTopicInfo),
            FavorLevel = FavorLevel.None,
            Flags = new DialogResponseFlags(),
        };
    }
}
