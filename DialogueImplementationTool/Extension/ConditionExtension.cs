using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Extension;

public static class ConditionExtension {
    public static Condition ToConditionFloat(
        this ConditionData data,
        float comparisonValue = 1,
        bool or = false) {
        var condition = new ConditionFloat {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = comparisonValue,
            Data = data,
        };

        if (or) condition.Flags = Condition.Flag.OR;

        return condition;
    }
}
