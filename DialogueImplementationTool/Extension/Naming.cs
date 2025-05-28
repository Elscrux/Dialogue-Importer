using System;
using System.Text.RegularExpressions;
namespace DialogueImplementationTool.Extension;

public static partial class Naming {
    public static string GetFirstFreeIndex(
        Func<int, string> selector,
        Func<string, bool> isFree,
        int start,
        int end = int.MaxValue) {
        for (var i = start; i < end; i++) {
            var index = selector(i);
            if (isFree(index)) return index;
        }

        throw new InvalidOperationException($"Could not find free index for {selector(start)}");
    }

    [GeneratedRegex(@"[^\w\d]")]
    private static partial Regex EditorIDRegex { get; }

    /// <summary>
    /// Sanitizes a string to be used as an Editor ID.
    /// </summary>
    /// <param name="str">string to sanitize</param>
    /// <returns>A sanitized string that is a valid EditorID.</returns>
    public static string ToEditorIDString(string str) {
        return EditorIDRegex.Replace(str, string.Empty);
    }
}
