using System;
using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public static class CompareOperatorUtils {
    [StringSyntax("Regex")] 
    public const string CompareOperatorRegex = "(>|<|=|>=|=>|≥|<=|=<|≤)";

    public static CompareOperator GetCompareOperator(string text) {
        return text switch {
            ">" => CompareOperator.GreaterThan,
            "<" => CompareOperator.LessThan,
            "=" => CompareOperator.EqualTo,
            ">=" => CompareOperator.GreaterThanOrEqualTo,
            "=>" => CompareOperator.GreaterThanOrEqualTo,
            "≥" => CompareOperator.GreaterThanOrEqualTo,
            "<=" => CompareOperator.LessThanOrEqualTo,
            "=<" => CompareOperator.LessThanOrEqualTo,
            "≤" => CompareOperator.LessThanOrEqualTo,
            _ => throw new InvalidOperationException(),
        };
    }
}
