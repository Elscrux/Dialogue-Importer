using System;
using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Services;

public sealed class AutomaticSpeakerSelection(
    ILinkCache linkCache,
    ISpeakerFavoritesSelection speakerFavoritesSelection)
    : ISpeakerSelection {
    public IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IReadOnlyList<string> speakerNames) {
        var aliasSpeakers = new List<AliasSpeaker>();

        foreach (var speakerName in speakerNames) {
            if (speakerName.Length < 3) continue;

            // Try to find the NPC by the editor ID
            var count = 0;
            INpcGetter? currentNpc = null;
            foreach (var npc in linkCache.PriorityOrder.WinningOverrides<INpcGetter>()) {
                if (npc.EditorID is null) continue;
                if (!npc.EditorID.Contains(speakerName, StringComparison.Ordinal)
                 && (npc.Name?.String is null || !npc.Name.String.Contains(speakerName, StringComparison.Ordinal))) continue;

                count++;
                if (count > 1) break;

                currentNpc = npc;
            }

            if (count == 1 && currentNpc is not null) {
                aliasSpeakers.Add(new AliasSpeaker(currentNpc.ToLinkGetter(), speakerName, editorId: currentNpc.EditorID));
            } else {
                // Try to find the NPC in the speaker favorites
                var closestSpeaker = speakerFavoritesSelection.GetClosestSpeaker(speakerName);
                if (closestSpeaker is not null) {
                    aliasSpeakers.Add(new AliasSpeaker(closestSpeaker.FormLink, speakerName, editorId: closestSpeaker.EditorID));
                }
            }
        }

        return aliasSpeakers;
    }
}
