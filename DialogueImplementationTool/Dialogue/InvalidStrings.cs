using System.Collections.Generic;
namespace DialogueImplementationTool;

public static class InvalidString {
	public static Dictionary<string, string> InvalidStrings { get; } = new() {
		{ "\r", "" },
		{ "\n", "" },
		{ "’", "'" },
		{ "`", "'" },
		{ "”", "\"" },
		{ "“", "\"" },
		{ "…", "..." },
		{ "—", "-" },
		{ "  ", " " },
	};
}
