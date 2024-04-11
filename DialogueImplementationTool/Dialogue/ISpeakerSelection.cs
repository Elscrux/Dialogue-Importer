using System.Collections.Generic;
namespace DialogueImplementationTool.Dialogue;

public interface ISpeakerSelection {
    IReadOnlyList<AliasSpeaker> GetAliasSpeakers(IEnumerable<string> speakerNames);
}
