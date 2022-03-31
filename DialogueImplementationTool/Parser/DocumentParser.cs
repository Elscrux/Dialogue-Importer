using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DialogueImplementationTool.Dialogue;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
namespace DialogueImplementationTool.Parser; 

public abstract class DocumentParser {
    public static readonly DocumentParser Null = new NullDocumentParser();
    private static readonly Dictionary<string, string> InvalidStrings = new() {
        {"\r", ""},
        {"’", "'"},
        {"`", "'"},
        {"”", "\""},
        {"…", "..."},
    };

    /*====================================================
		Iterator
	====================================================*/
    private int _index;
    public int Index {
        get => _index;
        protected set {
            _index = value;
            App.DialogueVM.Index = value;
        }
    }
    public abstract int LastIndex { get; }
    
    public void Previous() {
        if (Index > 0) Index--;
    }
    public void Next() {
        if (Index < LastIndex) Index++;
    }
    public abstract void SkipMany();
    public abstract void BacktrackMany();

    public string PreviewCurrent() => Preview(Index);
    public abstract string Preview(int index);


    /*====================================================
		Parsing
	====================================================*/
    private static readonly Dictionary<string, Type> DocumentParsers = new() {
        { ".odt", typeof(OpenDocumentTextParser) },
        { ".docx", typeof(DocXTextParser) },
    };

    private static DocumentParser? CreateParser(string file) {
        var extension = Path.GetExtension(file).ToLower();
        if (!DocumentParsers.ContainsKey(extension)) return null;

        return (DocumentParser?) Activator.CreateInstance(DocumentParsers[extension], file);
    }
    
    public static DocumentParser? LoadDocument() {
        var filterBuilder = new StringBuilder();
        var fileTypes = DocumentParsers.Keys.ToList();
        for (var index = 0; index < fileTypes.Count; index++) {
            filterBuilder.Append('*');
            filterBuilder.Append(fileTypes[index]);
            if (index != fileTypes.Count - 1) filterBuilder.Append(';');
        }

        var filter = filterBuilder.ToString();
        var fileDialog = new OpenFileDialog {
            Multiselect = false,
            Filter = $"Documents({filter})|{filter}"
        };

        return fileDialog.ShowDialog() is null or false ? null : CreateParser(fileDialog.FileName);
    }
    
    public static IEnumerable<DocumentParser> LoadDocuments() {
        var folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() != DialogResult.OK) yield break;
        
        foreach (var file in Directory.GetFiles(folderDialog.SelectedPath)) {
            var documentParser = CreateParser(file);
            if (documentParser != null) yield return documentParser;
        }
    }

    public List<GeneratedDialogue> GetDialogue() {
        var dialogue = new List<GeneratedDialogue>();
        for (var i = 0; i < App.DialogueVM.DialogueTypeList.Count; i++) {
            var (selection, speakerFormKey) = App.DialogueVM.DialogueTypeList[i];
            foreach (var (dialogueType, selected) in selection) {
                if (selected) dialogue.Add(new GeneratedDialogue(dialogueType, ParseDialogue(dialogueType, i), speakerFormKey));
            }
        }
        
        foreach (var (_, dialogueTopics, _) in dialogue) {
            foreach (var dialogueTopic in dialogueTopics) {
                for (var index = 0; index < dialogueTopic.Responses.Count; index++) {
                    foreach (var (invalid, fix) in InvalidStrings) {
                        dialogueTopic.Responses[index] = dialogueTopic.Responses[index].Replace(invalid, fix);
                    }
                }
            }
        }

        return dialogue;
    }

    private List<DialogueTopic> ParseDialogue(DialogueType dialogueType, int index) {
        switch (dialogueType) {
            case DialogueType.Dialogue:
                return ParseDialogue(index);
            case DialogueType.Greeting:
            case DialogueType.Farewell:
            case DialogueType.Idle:
                return ParseOneLiner(index);
            case DialogueType.GenericScene:
            case DialogueType.QuestScene:
                return ParseScene(index);
            default:
                throw new ArgumentOutOfRangeException(nameof(dialogueType), dialogueType, null);
        }
    }
    protected abstract List<DialogueTopic> ParseDialogue(int index);
    protected abstract List<DialogueTopic> ParseOneLiner(int index);
    protected abstract List<DialogueTopic> ParseScene(int index);
}