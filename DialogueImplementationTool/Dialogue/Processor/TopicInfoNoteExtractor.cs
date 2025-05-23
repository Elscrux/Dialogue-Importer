﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class TopicInfoNoteExtractor : IDialogueTopicInfoProcessor {
    public void Process(DialogueTopicInfo topicInfo) {
        ProcessNotes(
            topicInfo,
            NoteUtils.StartNoteRegex,
            StringExt.TrimStart,
            topicInfo.Prompt.StartNotes);

        ProcessNotes(
            topicInfo,
            NoteUtils.EndNoteRegex,
            StringExt.TrimEnd,
            topicInfo.Prompt.EndsNotes);
    }

    private static void ProcessNotes(
        DialogueTopicInfo topicInfo,
        Regex regex,
        Func<string, string, string> trim,
        List<Note> notes) {
        var match = regex.Match(topicInfo.Prompt.Text);
        while (match.Success) {
            // Trim text
            var newResponse = trim(trim(topicInfo.Prompt.Text, match.Value), " ");
            topicInfo.Prompt.Text = newResponse;

            // Add notes
            notes.Add(new Note {
                Text = match.Groups[1].Value.Trim(),
                Colors = [Color.Black,],
            });

            match = regex.Match(topicInfo.Prompt.Text);
        }
    }
}
