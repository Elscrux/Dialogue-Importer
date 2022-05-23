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
        var nameMappings = new Dictionary<FormKey, string>();
        foreach (var (type, topics, speaker) in dialogue) {
            if (topics.Count == 0 || !DialogueFactories.ContainsKey(type)) continue;

            var name = string.Empty;
            if (speaker != FormKey.Null) {
                if (nameMappings.ContainsKey(speaker)) {
                    name = nameMappings[speaker];
                } else {
                    if (Environment.LinkCache.TryResolve<INpcGetter>(speaker, out var named)) {
                        //Remove white spaces from name
                        name = WhitespaceRegex.Replace(named.Name?.String ?? string.Empty, string.Empty);
                        
                        nameMappings.Add(named.FormKey, name);
                    }
                }
            }
            
            DialogueFactories[type].GenerateDialogue(topics, speaker, name);
        }
        
        //Do post processing
        foreach (var factory in DialogueFactories.Values) factory.PostProcess();
    }
}