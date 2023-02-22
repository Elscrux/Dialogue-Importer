using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Dialogue.Topics;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
namespace DialogueImplementationTool.Parser; 

public abstract class DocumentParser {
    public static readonly DocumentParser Null = new NullDocumentParser();

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

    public static string GetFilter() {
        var filterBuilder = new StringBuilder();
        var fileTypes = DocumentParsers.Keys.ToList();
        for (var index = 0; index < fileTypes.Count; index++) {
            filterBuilder.Append('*');
            filterBuilder.Append(fileTypes[index]);
            if (index != fileTypes.Count - 1) filterBuilder.Append(';');
        }

        return filterBuilder.ToString();
    }
    
    public static DocumentParser? LoadDocument() {
        var filter = GetFilter();
        var fileDialog = new OpenFileDialog {
            Multiselect = false,
            Filter = $"Documents({filter})|{filter}"
        };

        return fileDialog.ShowDialog() is null or false ? null : CreateParser(fileDialog.FileName);
    }
    
    public static IEnumerable<DocumentParser> LoadDocuments() {
        var folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() != DialogResult.OK) yield break;

        var extensions = DocumentParsers.Keys.ToList();
        foreach (var file in Directory.EnumerateFiles(folderDialog.SelectedPath, "*.*", SearchOption.AllDirectories)
            .Where(file => extensions.Any(file.EndsWith))) {
            var documentParser = CreateParser(file);
            if (documentParser != null) yield return documentParser;
        }
    }

    public List<GeneratedDialogue> GetDialogue() {
        var dialogue = new List<GeneratedDialogue>();
        for (var i = 0; i < App.DialogueVM.DialogueTypeList.Count; i++) {
            var (selection, speakerFormKey) = App.DialogueVM.DialogueTypeList[i];
            foreach (var (dialogueType, selected) in selection) {
                if (selected) {
                    var dialogueTopics = ParseDialogue(dialogueType, i);
                    foreach (var rootTopic in dialogueTopics) {
                        foreach (var topic in rootTopic.EnumerateLinks()) {
                            topic.PostProcess();
                        }
                    }
                    
                    dialogue.Add(new GeneratedDialogue(dialogueType, dialogueTopics, speakerFormKey));
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