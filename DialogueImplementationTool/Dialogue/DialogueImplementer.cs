using System.Collections.Generic;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public class DialogueImplementer {
    public static readonly IGameEnvironmentState<ISkyrimMod, ISkyrimModGetter> Environment = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
    private static readonly Regex WhitespaceRegex = new(@"\s+");
    public static IQuestGetter Quest = new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);

    private static readonly Dictionary<DialogueType, DialogueFactory> DialogueFactories = new() {
        { DialogueType.Greeting, new Greeting() },
        { DialogueType.Farewell, new Farewell() },
        { DialogueType.Idle, new Idle() },
        { DialogueType.Dialogue, new Dialogue() },
        { DialogueType.GenericScene, new GenericScene() },
        { DialogueType.QuestScene, new QuestScene() }
    };

    public DialogueImplementer(FormKey questFormKey) {
        Quest = questFormKey != FormKey.Null ? Environment.LinkCache.Resolve<IQuestGetter>(questFormKey) : new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);
    }

    public void ImplementDialogue(List<GeneratedDialogue> dialogue) {
        DialogueFactory.Mod.Clear();
        var linkCache = Environment.LinkCache;

        var npcMappings = new Dictionary<FormKey, INpcGetter>();
        foreach (var (type, topics, speaker) in dialogue) {
            if (topics.Count == 0 || !DialogueFactories.ContainsKey(type)) continue;

            var name = string.Empty;
            if (speaker != FormKey.Null) {
                //Get npc record
                INpcGetter npc;
                if (npcMappings.ContainsKey(speaker)) {
                    npc = npcMappings[speaker];
                } else {
                    npc = linkCache.Resolve<INpcGetter>(speaker);
                    npcMappings.Add(speaker, npc);
                }
            
                //Remove white spaces from name
                name = WhitespaceRegex.Replace(npc.Name?.String ?? string.Empty, string.Empty);
            }
            
            DialogueFactories[type].GenerateDialogue(topics, speaker, name);
        }
    }
}