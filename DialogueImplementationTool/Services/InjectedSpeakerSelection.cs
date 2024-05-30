using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Speaker;
namespace DialogueImplementationTool.Services;

public sealed class InjectedSpeakerSelection(IReadOnlyDictionary<string, AliasSpeaker> aliases) : ISpeakerSelection {
    public IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IEnumerable<string> speakerNames) {
        return speakerNames.Select(x => aliases[x]).ToList();
    }
}
