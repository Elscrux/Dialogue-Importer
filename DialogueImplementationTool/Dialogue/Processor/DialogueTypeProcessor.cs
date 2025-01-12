using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public abstract class DialogueTypeProcessor : IGenericDialogueProcessor {
    /// <summary>
    /// Passed as placeholder in case a description is valid but has no conditions
    /// </summary>
    protected static readonly Condition NullCondition = new ConditionFloat();

    public void Process(GenericDialogue genericDialogue, DialogueTopicInfo topicInfo) {
        if (topicInfo.MetaData[GenericMetaData.Subtype] is not DialogTopic.SubtypeEnum subtype)
            throw new InvalidOperationException("Subtype not found");

        if (!IsApplicable(subtype)) return;

        var conditions = GetConditions(genericDialogue.Description, topicInfo).ToList();

        if (conditions.Count > 0) {
            // When a matching condition is found, remove the description from the topic
            topicInfo.MetaData.Remove(GenericMetaData.Description);
        } else {
            // Otherwise, add the description as a note to the topic to be implemented manually
            var note = new Note { Text = genericDialogue.Description };
            topicInfo.Responses[0].StartNotes.Add(note);
        }

        topicInfo.ExtraConditions.AddRange(conditions.Where(c => !ReferenceEquals(c, NullCondition)));
    }

    protected abstract bool IsApplicable(DialogTopic.SubtypeEnum subtype);
    protected abstract IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo);
}
