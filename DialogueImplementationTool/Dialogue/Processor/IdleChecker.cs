using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class IdleChecker(IDialogueContext context) : IDialogueResponseProcessor {
    public void Process(DialogueResponse response, IList<FormattedText> textSnippets) {
        foreach (var note in response.Notes()) {
            if (!context.LinkCache.TryResolve<IIdleAnimationGetter>(note.Text, out var idle)) continue;

            response.SpeakerIdle = idle.FormKey;
            response.RemoveNote(note);
        }
    }
}
