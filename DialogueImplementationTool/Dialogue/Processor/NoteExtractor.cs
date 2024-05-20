using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Parser;
using Noggog;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed partial class NoteExtractor : IDialogueResponseProcessor {
    private const string NotePattern = @"\[([^\]]*)\]";

    [GeneratedRegex($"^{NotePattern}")]
    private static partial Regex StartNoteRegex();

    [GeneratedRegex($"{NotePattern}$")]
    private static partial Regex EndNoteRegex();

    public void Process(DialogueResponse response, IReadOnlyList<FormattedText> textSnippets) {
        var processedSnippets = textSnippets.ToList();

        ProcessNotes(
            StartNoteRegex(),
            StringExt.TrimStart,
            (s, i) => s[..i],
            response.StartNotes,
            processedSnippets);
        ProcessNotes(
            EndNoteRegex(),
            StringExt.TrimEnd,
            (s, i) => s[..^i],
            response.EndsNotes,
            (processedSnippets as IEnumerable<FormattedText>).Reverse().ToList());

        void ProcessNotes(
            Regex regex,
            Func<string, string, string> trim,
            Func<string, int, string> subString,
            List<Note> notes,
            IReadOnlyList<FormattedText> snippetOrder) {
            var match = regex.Match(response.Response);
            while (match.Success) {
                // Trim text
                var newResponse = trim(trim(response.Response, match.Value), " ");
                var charsToRemove = response.Response.Length - newResponse.Length;
                response.Response = newResponse;

                // Check which colors are used in matched text
                var colors = new List<Color>();
                var newSnippets = new List<FormattedText>();
                foreach (var snippet in snippetOrder) {
                    // Skip chars and add remaining snippet (parts)
                    if (charsToRemove == 0) {
                        newSnippets.Add(snippet);
                    } else if (snippet.Text.Length <= charsToRemove) {
                        charsToRemove -= snippet.Text.Length;
                        colors.Add(snippet.Color);
                    } else {
                        newSnippets.Add(snippet with { Text = subString(snippet.Text, charsToRemove) });
                        charsToRemove = 0;
                        colors.Add(snippet.Color);
                    }
                }

                processedSnippets = newSnippets;

                // Add note
                notes.Add(new Note {
                    Text = match.Groups[1].Value,
                    Colors = colors,
                });

                match = regex.Match(response.Response);
            }
        }
    }
}
