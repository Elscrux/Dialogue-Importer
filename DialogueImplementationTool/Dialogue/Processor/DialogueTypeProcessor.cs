﻿using System.Collections.Generic;
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
        var subtype = GenericMetaData.GetSubtype(topicInfo.MetaData);

        if (!IsApplicable(subtype)) return;

        var description = genericDialogue.Description.Trim();
        var conditions = GetConditions(description, topicInfo).ToList();

        if (conditions.Count > 0) {
            // When a matching condition is found, remove the description from the topic
            topicInfo.MetaData.Remove(GenericMetaData.Description);
        } else {
            // Otherwise, add the description as a note to the topic to be implemented manually
            var note = new Note { Text = description };
            topicInfo.Responses[0].StartNotes.Add(note);
        }

        topicInfo.ExtraConditions.AddRange(conditions.Where(c => !ReferenceEquals(c, NullCondition)));
    }

    protected abstract bool IsApplicable(DialogTopic.SubtypeEnum subtype);
    protected abstract IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo);
}
