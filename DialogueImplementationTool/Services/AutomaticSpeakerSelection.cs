using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Services;

public sealed class AutomaticSpeakerSelection(
    ILinkCache linkCache,
    IPrefixProvider prefixProvider,
    ISpeakerFavoritesSelection speakerFavoritesSelection)
    : ISpeakerSelection {
    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames)
        where T : class, ISpeaker {
        return GetSpeakers<T>(speakerNames, true);
    }

    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames, bool exactMatch)
        where T : class, ISpeaker {
        var speakers = new List<T>();

        // Depending on type of T, switch constructor
        Func<IFormLinkGetter, string, string?, T> createSpeaker = typeof(T) == typeof(AliasSpeaker)
            ? (formLink, name, editorId) => (new AliasSpeaker(formLink, name, editorId: editorId) as T)!
            : (formLink, _, _) => (new NpcSpeaker(linkCache, formLink) as T)!;

        foreach (var speakerName in speakerNames) {
            // Skip short names, they cannot be reliably linked, usually this is just the starting letter of the name
            if (speakerName.Length < 3) continue;

            // Try to find the NPC by the editor ID
            var npcs = linkCache.PriorityOrder.WinningOverrides<INpcGetter>()
                .Where(npc => NameMatches(npc.EditorID, speakerName) || NameMatches(npc.Name?.String, speakerName));

            if (!npcs.CountGreaterThan(1)) {
                var npc = npcs.FirstOrDefault();
                if (npc is null) continue;

                speakers.Add(createSpeaker(npc.ToLinkGetter(), speakerName, npc.EditorID));
            } else {
                // Only attempt to find the closest match when an exact match is not required
                if (exactMatch) continue;

                // Try to find the NPC in the speaker favorites
                var closestSpeaker = speakerFavoritesSelection.GetClosestSpeakers(speakerName).FirstOrDefault();
                if (closestSpeaker is not null) {
                    speakers.Add(createSpeaker(closestSpeaker.FormLink, speakerName, closestSpeaker.EditorID));
                } else {
                    var closestNpc = npcs
                        .MinBy(npc => {
                            var index = npc.EditorID?.IndexOf(speakerName, StringComparison.Ordinal);
                            if (index is null or -1) return int.MaxValue;

                            // Reward matches that start with the prefix
                            if (npc.EditorID?.StartsWith(prefixProvider.Prefix) is true) {
                                index -= prefixProvider.Prefix.Length + 1;
                            }

                            return index;
                        });

                    if (closestNpc?.EditorID?.Contains((speakerName), StringComparison.OrdinalIgnoreCase) is true) {
                        speakers.Add(createSpeaker(closestNpc.ToLinkGetter(), speakerName, closestNpc.EditorID));
                    }
                }
            }
        }

        return speakers;
    }

    private static bool NameMatches(string? name, string reference) {
        if (name is null) return false;

        return name.Contains(reference, StringComparison.OrdinalIgnoreCase);
    }
}
