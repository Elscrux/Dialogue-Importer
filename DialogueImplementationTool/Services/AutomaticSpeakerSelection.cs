using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Services;

public sealed class AutomaticSpeakerSelection(
    ILinkCache linkCache,
    ISpeakerFavoritesSelection speakerFavoritesSelection)
    : ISpeakerSelection {
    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames) where T : class, ISpeaker {
        var aliasSpeakers = new List<T>();
        
        // Depending on type of T, switch constructor
        Func<IFormLinkGetter, string, string?, T> createSpeaker = typeof(T) == typeof(AliasSpeaker)
            ? (formLink, name, editorId) => (new AliasSpeaker(formLink, name, editorId: editorId) as T)!
            : (formLink, _, _) => (new NpcSpeaker(linkCache, formLink) as T)!;

        foreach (var speakerName in speakerNames) {
            // Skip short names, they cannot be reliably linked, usually this is just the starting letter of the name
            if (speakerName.Length < 3) continue;

            // Try to find the NPC by the editor ID
            var count = 0;
            INpcGetter? currentNpc = null;
            foreach (var npc in linkCache.PriorityOrder.WinningOverrides<INpcGetter>()) {
                if (npc.EditorID is null) continue;
                if (!npc.EditorID.Contains(speakerName, StringComparison.OrdinalIgnoreCase)
                 && (npc.Name?.String is null || !npc.Name.String.Contains(speakerName, StringComparison.OrdinalIgnoreCase))) continue;

                count++;
                if (count > 1) break;

                currentNpc = npc;
            }

            if (count == 1 && currentNpc is not null) {
                aliasSpeakers.Add(createSpeaker(currentNpc.ToLinkGetter(), speakerName, currentNpc.EditorID));
            } else {
                // Try to find the NPC in the speaker favorites
                var closestSpeaker = speakerFavoritesSelection.GetClosestSpeaker(speakerName);
                if (closestSpeaker is not null) {
                    aliasSpeakers.Add(createSpeaker(closestSpeaker.FormLink, speakerName, closestSpeaker.EditorID));
                }
            }
        }

        return aliasSpeakers;
    }
}
