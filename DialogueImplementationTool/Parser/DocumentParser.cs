using System.Collections.Generic;
using System.IO;
using DialogueImplementationTool.Dialogue;
using Microsoft.Win32;
namespace DialogueImplementationTool.Parser; 

public abstract class DocumentParser {
    public static DocumentParser? LoadDocument() {
        var fileDialog = new OpenFileDialog {
            Multiselect = false,
            Filter = "Documents(*.ODT)|*.ODT"
        };
        
        if (fileDialog.ShowDialog() is null or false) return null;
        
        return Path.GetExtension(fileDialog.FileName).ToLower() switch {
            ".odt" => new OpenDocumentTextParser(fileDialog.FileName),
            _ => null
        };
    }

    public abstract List<DialogueTopic> ParseNext();

    public abstract string PreviewCurrent();
    public abstract bool HasFinished();
    public abstract void SkipOne();
    public abstract void SkipMany();
}
