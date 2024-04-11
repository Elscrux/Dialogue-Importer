using System;
namespace DialogueImplementationTool.Extension;

public static class Naming {
    public static string GetFirstFreeIndex(
        Func<int, string> selector,
        Func<string, bool> isFree,
        int start = 0,
        int end = int.MaxValue) {
        for (var i = start; i < end; i++) {
            var index = selector(i);
            if (isFree(index)) return index;
        }

        throw new InvalidOperationException($"Could not find free index for {selector(start)}");
    }
}
