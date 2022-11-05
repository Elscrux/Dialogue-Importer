using System.Collections.Generic;
namespace DialogueImplementationTool; 

public class InvalidString {
    public static readonly Dictionary<string, string> InvalidStrings = new() {
        {"\r", ""},
        {"\n", ""},
        {"’", "'"},
        {"`", "'"},
        {"”", "\""},
        {"…", "..."},
    };
}
