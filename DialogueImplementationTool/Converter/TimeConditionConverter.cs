﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Converter;

public static partial class TimeConditionConverter {
    [StringSyntax(StringSyntaxAttribute.Regex)]
    private const string HourPattern = @"\b(\d{1,2})(?::(\d{2}))?\b";

    [GeneratedRegex(@$"{HourPattern}[^\d]*{HourPattern}")]
    private static partial Regex TimeRegex { get; }

    public static IEnumerable<Condition> Convert(string time) {
        var match = TimeRegex.Match(time);
        if (!match.Success) yield break;

        if (!int.TryParse(match.Groups[1].Value, out var startHour)
         || !int.TryParse(match.Groups[3].Value, out var endHour)) yield break;

        // Minutes are optional - assume 0 if not present
        var startMinutes = int.TryParse(match.Groups[2].Value, out var s) ? s : 0;
        var endMinutes = int.TryParse(match.Groups[4].Value, out var e) ? e : 0;

        if (startHour < endHour) {
            // For example "02:00 - 22:00" is >= 02:00 AND <= 22:00
            yield return GetCondition(startHour, startMinutes, CompareOperator.GreaterThanOrEqualTo);
            yield return GetCondition(endHour, endMinutes, CompareOperator.LessThanOrEqualTo);
        } else {
            // For example "22:00 - 02:00" is >= 22:00 OR <= 02:00
            var startCondition = GetCondition(startHour, startMinutes, CompareOperator.GreaterThanOrEqualTo);
            startCondition.Flags |= Condition.Flag.OR;
            yield return startCondition;
            yield return GetCondition(endHour, endMinutes, CompareOperator.LessThanOrEqualTo);
            
        }
    }

    private static ConditionFloat GetCondition(int hour, int minutes, CompareOperator compareOperator) {
        return new ConditionFloat {
            Data = new GetCurrentTimeConditionData(),
            ComparisonValue = hour + minutes / 60f,
            CompareOperator = compareOperator
        };
    }
}
