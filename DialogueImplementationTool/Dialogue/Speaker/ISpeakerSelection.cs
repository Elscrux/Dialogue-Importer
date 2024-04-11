using System.Collections.Generic;
namespace DialogueImplementationTool.Dialogue.Speaker;

public interface ISpeakerSelection {
    IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IEnumerable<string> speakerNames);
}
