using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Speaker;
using Noggog;
namespace DialogueImplementationTool.Services;

public sealed class InjectedSpeakerSelection(IReadOnlyDictionary<string, ISpeaker> speakers) : ISpeakerSelection {
    public IReadOnlyList<T> GetSpeakers<T>(IReadOnlyList<string> speakerNames)
        where T : class, ISpeaker {
        return speakerNames.Select(x => speakers[x] as T).NotNull().ToList();
    }
}
