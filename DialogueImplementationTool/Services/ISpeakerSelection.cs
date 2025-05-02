using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Speaker;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
namespace DialogueImplementationTool.Services;

public interface ISpeakerSelection {
    IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames)
        where T : class, ISpeaker;

    static T CreateSpeaker<T>(ILinkCache linkCache, IFormLinkGetter formLink, string name, string? editorId)
        where T : class, ISpeaker {
        return typeof(T) == typeof(AliasSpeaker)
            ? (new AliasSpeaker(formLink, name, editorId: editorId) as T)!
            : (new NpcSpeaker(linkCache, formLink) as T)!;
    }
}
