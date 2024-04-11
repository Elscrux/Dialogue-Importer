using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Extension;

public static class NpcExtension {
    public static string GetName(this INpcGetter npc) {
        return npc.ShortName?.String ?? npc.Name?.String ?? npc.EditorID ?? npc.FormKey.ToString();
    }
}
