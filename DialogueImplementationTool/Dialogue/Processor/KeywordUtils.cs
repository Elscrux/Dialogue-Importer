using System.Diagnostics.CodeAnalysis;
namespace DialogueImplementationTool.Dialogue.Processor;

public static class KeywordUtils {
    [StringSyntax("Regex")] public const string KeywordRegexPart = @"([A-Z-_\d]{2,})";
}
