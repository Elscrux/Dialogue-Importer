using System;
using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Extension;

public static class CompareOperatorExtension {
    [StringSyntax("Regex")] 
    public const string CompareOperatorRegex = "(?<compare>>|<|=|>=|=>|≥|<=|=<|≤)";

    public static CompareOperator GetCompareOperator(string text) {
        return text switch {
            ">" => CompareOperator.GreaterThan,
            "<" => CompareOperator.LessThan,
            "=" => CompareOperator.EqualTo,
            ">=" or "=>" or "≥" => CompareOperator.GreaterThanOrEqualTo,
            "<=" or "=<" or "≤" => CompareOperator.LessThanOrEqualTo,
            _ => throw new InvalidOperationException(),
        };
    }
    
    public static CompareOperator Negate(this CompareOperator compareOperator) {
        return compareOperator switch {
            CompareOperator.GreaterThan => CompareOperator.LessThanOrEqualTo,
            CompareOperator.LessThan => CompareOperator.GreaterThanOrEqualTo,
            CompareOperator.GreaterThanOrEqualTo => CompareOperator.LessThan,
            CompareOperator.LessThanOrEqualTo => CompareOperator.GreaterThan,
            CompareOperator.EqualTo => CompareOperator.NotEqualTo,
            CompareOperator.NotEqualTo => CompareOperator.EqualTo,
            _ => throw new InvalidOperationException(),
        };
    }
}
