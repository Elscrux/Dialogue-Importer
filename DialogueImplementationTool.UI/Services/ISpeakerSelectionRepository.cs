using DialogueImplementationTool.UI.Models;
namespace DialogueImplementationTool.UI.Services;

public interface ISpeakerSelectionRepository {
    void SaveSceneSpeakers(IReadOnlyList<AliasSpeakerSelection> speakers);
    bool TryLoadSceneSpeakers(IReadOnlyList<string> speakerNames, out List<AliasSpeakerSelection> speakerSelections);
}
