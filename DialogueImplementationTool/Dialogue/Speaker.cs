using System;
using System.Linq;
using System.Text.RegularExpressions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.Dialogue;

public interface ISpeaker {
    public FormKey FormKey { get; set; }
    public string? EditorID { get; set; }
    public string Name { get; set; }
}

public record Speaker : ISpeaker {
    private static readonly Regex WhitespaceRegex = new(@"\s+");
    
    public FormKey FormKey { get; set; }
    public string? EditorID { get; set; }
    public string Name { get; set; }

    public Speaker(FormKey formKey) {
        FormKey = formKey;
        
        if (App.DialogueVM.LinkCache.TryResolve<INpcGetter>(FormKey, out var npc)) {
            EditorID = npc.EditorID;
            Name = WhitespaceRegex.Replace(npc.Name?.String ?? string.Empty, string.Empty);
        } else {
            Name = EditorID = string.Empty;
        }
    }
}

public class AliasSpeaker : ReactiveObject, ISpeaker {
    public AliasSpeaker(string name) {
        Name = name;

        this.WhenAnyValue(x => x.FormKey)
            .Subscribe(_ => { 
                if (FormKey != FormKey.Null && App.DialogueVM.SpeakerFavourites.All(s => s.FormKey != FormKey)) {
                    App.DialogueVM.SpeakerFavourites.Add(new Speaker(FormKey));
                }
            });
    }

    public string Name { get; set; }
    [Reactive] public FormKey FormKey { get; set; }
    public string? EditorID { get; set; }
    public int AliasIndex { get; set; } = -1;
}
