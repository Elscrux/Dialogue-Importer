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
}
