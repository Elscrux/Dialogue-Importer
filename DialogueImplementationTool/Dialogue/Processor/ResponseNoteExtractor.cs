﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class ResponseNoteExtractor : IDialogueResponseProcessor {
    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
        var processedSnippets = textSnippets.ToList();

        ProcessNotes(
            NoteUtils.StartNoteRegex,
            StringExt.TrimStart,
            (s, i) => s[i..],
            response.StartNotes,
            x => x);
        ProcessNotes(
            NoteUtils.EndNoteRegex,
            StringExt.TrimEnd,
            (s, i) => s[..^i],
            response.EndsNotes,
            x => x.Reverse());

        void ProcessNotes(
            Regex regex,
            Func<string, string, string> trim,
            Func<string, int, string> subString,
            List<Note> notes,
            Func<IEnumerable<FormattedText>, IEnumerable<FormattedText>> orderSnippets) {
            processedSnippets = orderSnippets(processedSnippets).ToList();

            var match = regex.Match(response.Response);
            while (match.Success) {
                // Trim text
                var withWhitespaces = trim(response.Response, match.Value);
                var newResponse = trim(withWhitespaces, " ");
                var charsToRemove = response.Response.Length - newResponse.Length;
                response.Response = newResponse;

                // Check which colors are used in matched text
                var colors = new List<Color>();
                var newSnippets = new List<FormattedText>();
                foreach (var snippet in processedSnippets) {
                    // Skip chars and add remaining snippet (parts)
                    if (charsToRemove == 0) {
                        newSnippets.Add(snippet);
                    } else if (snippet.Text.Length <= charsToRemove) {
                        charsToRemove -= snippet.Text.Length;
                        colors.Add(snippet.Color);
                    } else {
                        var trimmedText = subString(snippet.Text, charsToRemove);
                        newSnippets.Add(snippet with { Text = trimmedText });
                        charsToRemove = 0;

                        // Only if non-whitespace text was removed, add the color
                        if (trimmedText.Trim() != snippet.Text.Trim()) {
                            colors.Add(snippet.Color);
                        }
                    }
                }

                processedSnippets = newSnippets;

                // Add notes
                notes.Add(new Note {
                    Text = match.Groups[1].Value.Trim(),
                    Colors = colors,
                });

                match = regex.Match(response.Response);
            }
        }
    }
}
