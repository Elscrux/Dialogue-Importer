using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using ISpeaker = DialogueImplementationTool.Dialogue.Speaker.ISpeaker;
namespace DialogueImplementationTool.Parser;

public sealed class GeneratedDialogue {
    public BaseDialogueFactory Factory { get; }
    public List<DialogueTopic> Topics { get; }

    public GeneratedDialogue(
        IDialogueContext context,
        BaseDialogueFactory factory,
        List<DialogueTopic> topics,
        FormKey speakerFormKey,
        bool useGetIsAliasRef = false) {
        Factory = factory;
        Topics = topics;

        ISpeaker speaker;
        if (useGetIsAliasRef) {
            var alias = context.Quest.GetOrAddAlias(context.LinkCache, speakerFormKey);
            speaker = new AliasSpeaker(speakerFormKey, alias.Name!, (int) alias.ID);
        } else {
            speaker = new NpcSpeaker(context.LinkCache, speakerFormKey);
        }

        //Set speaker for all linked topics
        foreach (var topic in topics.EnumerateLinks(true)) {
            foreach (var topicInfo in topic.TopicInfos) {
                topicInfo.Speaker = speaker;
            }
        }
    }
}

public sealed class DialogueSelection {
    public Dictionary<DialogueType, bool> Selection { get; } =
        Enum.GetValues<DialogueType>().ToDictionary(type => type, _ => false);

    public FormKey Speaker { get; set; } = FormKey.Null;
    public bool UseGetIsAliasRef { get; set; }
}
