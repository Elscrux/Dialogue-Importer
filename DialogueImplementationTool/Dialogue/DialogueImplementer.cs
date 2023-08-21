using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Conversation;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public sealed class DialogueImplementer {
    public static readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> Environment = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
    public static IQuestGetter Quest = new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);
    public static IQuest OverrideQuest = null!;

    private static readonly IConversationProcessor[] ConversationProcessors = {
        new SharedInfoConverter(),
    };

    public static DialogueFactory GetDialogueFactory(DialogueType type) {
        return type switch {
            DialogueType.Dialogue => new Dialogue(),
            DialogueType.Greeting => new Greeting(),
            DialogueType.Farewell => new Farewell(),
            DialogueType.Idle => new Idle(),
            DialogueType.GenericScene => new GenericScene(),
            DialogueType.QuestScene => new QuestScene(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public DialogueImplementer(FormKey questFormKey) {
        Quest = questFormKey != FormKey.Null
            ? Environment.LinkCache.Resolve<IQuestGetter>(questFormKey)
            : new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);
        if (Quest.FormKey.IsNull) return;

        var questContext = Environment.LinkCache.ResolveContext<IQuest, IQuestGetter>(Quest.FormKey);
        OverrideQuest = questContext.GetOrAddAsOverride(DialogueFactory.Mod);
    }

    public void ImplementDialogue(List<GeneratedDialogue> dialogue) {
        foreach (var processor in ConversationProcessors) {
            processor.Process(dialogue);
        }

        dialogue.Where(d => d.Topics.Count > 0)
            .ForEach(d => d.Factory.PreProcess(d.Topics));

        dialogue.Where(d => d.Topics.Count > 0)
            .ForEach(d => d.Factory.GenerateDialogue(d.Topics));

        //Do post processing
        dialogue.Select(d => d.Factory)
            .ForEach(d => d.PostProcess());
    }
}