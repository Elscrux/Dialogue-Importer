using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Speaker;
namespace DialogueImplementationTool.Services;

public interface ISpeakerSelection {
    IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IEnumerable<string> speakerNames);
}
