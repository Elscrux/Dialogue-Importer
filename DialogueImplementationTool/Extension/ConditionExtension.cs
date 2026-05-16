using System.IO;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Extension;

public static class ConditionExtension {
    public static Condition ToConditionFloat(
        this ConditionData data,
        float comparisonValue = 1,
        CompareOperator compareOperator = CompareOperator.EqualTo,
        bool or = false) {
        var condition = new ConditionFloat {
            CompareOperator = compareOperator,
            ComparisonValue = comparisonValue,
            Data = data,
        };

        if (or) condition.Flags = Condition.Flag.OR;

        return condition;
    }

    public static Condition Negate(this Condition condition) {
        switch (condition) {
            case ConditionFloat conditionFloat:
                return new ConditionFloat {
                    Data = conditionFloat.Data,
                    CompareOperator = conditionFloat.CompareOperator.Negate(),
                    ComparisonValue = conditionFloat.ComparisonValue,
                };
            case ConditionGlobal conditionGlobal:
                return new ConditionGlobal {
                    Data = conditionGlobal.Data,
                    CompareOperator = conditionGlobal.CompareOperator.Negate(),
                    ComparisonValue = conditionGlobal.ComparisonValue,
                };
            default:
                throw new InvalidDataException(nameof(condition));
        }
    }
}
