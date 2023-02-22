using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using DialogueImplementationTool.Dialogue;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace DialogueImplementationTool.UI;

public class DialogueVM : ViewModel {
    public ILinkCache LinkCache { get; } = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE, LinkCachePreferences.OnlyIdentifiers()).LinkCache;
    public DocumentParser DocumentParser = DocumentParser.Null;
    public DialogueImplementer DialogueImplementer = new(FormKey.Null);

    /*====================================================
		Quest
	====================================================*/
    public IEnumerable<Type> QuestTypes { get; } = typeof(IQuestGetter).AsEnumerable();
    [Reactive]
    public FormKey QuestFormKey { get; set; } = FormKey.Null;
    [Reactive]
    public bool ValidQuest { get; set; }

    
    /*====================================================
		Dialogue List
	====================================================*/
    public List<DialogueSelection> DialogueTypeList { get; } = new();
    public ObservableCollection<Speaker> SpeakerFavourites { get; } = new();

    public bool SavedSession;

    [Reactive]
    public string PreviewText { get; set; } = string.Empty;

    [Reactive]
    public int Index { get; set; }
    
    [Reactive]
    public bool IsNotFirstIndex { get; set; }
    [Reactive]
    public bool IsNotLastIndex { get; set; }

    [Reactive]
    public bool GreetingSelected { get; set; }
    [Reactive]
    public bool FarewellSelected { get; set; }
    [Reactive]
    public bool IdleSelected { get; set; }
    [Reactive]
    public bool DialogueSelected { get; set; }
    [Reactive]
    public bool GenericSceneSelected { get; set; }
    [Reactive]
    public bool QuestSceneSelected { get; set; }

    
    /*====================================================
		NPC
	====================================================*/
    public IEnumerable<Type> SpeakerTypes { get; } = new List<Type> { typeof(INpcGetter), typeof(IFactionGetter) };
    [Reactive]
    public FormKey SpeakerFormKey { get; set; }
    [Reactive]
    public bool ValidSpeaker { get; set; }
    
    public ICommand SetSpeaker { get; }
    public ICommand SelectIndex { get; }
    public ICommand Save { get; }
    
    public ICommand BacktrackMany { get; }
    public ICommand Previous { get; }
    public ICommand Next { get; }
    public ICommand SkipMany { get; }

    public DialogueVM() {
        SetSpeaker = ReactiveCommand.Create((FormKey formKey) => SpeakerFormKey = formKey);
        SelectIndex = ReactiveCommand.Create<string>(indexStr => {
            switch (int.Parse(indexStr)) {
                case 1:
                    if (ValidSpeaker) GreetingSelected = !GreetingSelected;
                    break;
                case 2:
                    if (ValidSpeaker) FarewellSelected = !FarewellSelected;
                    break;
                case 3:
                    if (ValidSpeaker) IdleSelected = !IdleSelected;
                    break;
                case 4:
                    if (ValidSpeaker) DialogueSelected = !DialogueSelected;
                    break;
                case 5:
                    GenericSceneSelected = !GenericSceneSelected;
                    break;
                case 6:
                    QuestSceneSelected = !QuestSceneSelected;
                    break;
            }
        });
        Save = ReactiveCommand.Create(() => {
            App.DialogueVM.DialogueImplementer.ImplementDialogue(App.DialogueVM.DocumentParser.GetDialogue());
            DialogueFactory.Save();
            App.DialogueVM.SavedSession = true;
        });

        BacktrackMany = ReactiveCommand.Create(() => {
            DocumentParser.BacktrackMany();
            RefreshPreview(false);
        });

        Previous = ReactiveCommand.Create(() => {
            DocumentParser.Previous();
            RefreshPreview(false);
        });

        Next = ReactiveCommand.Create(() => {
            DocumentParser.Next();
            RefreshPreview(true);
        });

        SkipMany = ReactiveCommand.Create(() => {
            DocumentParser.SkipMany();
            RefreshPreview(true);
        });
        
        this.WhenAnyValue(v => v.Index)
            .Subscribe(_ => {
                IsNotFirstIndex = Index > 0;
                IsNotLastIndex = Index < DocumentParser.LastIndex;
                
                if (DialogueTypeList.Count > Index) {
                    if (DialogueTypeList[Index].Speaker != FormKey.Null) {
                        SpeakerFormKey = DialogueTypeList[Index].Speaker;                        
                    } else {
                        DialogueTypeList[Index].Speaker = SpeakerFormKey;
                    }
                    GreetingSelected = DialogueTypeList[Index].Selection[DialogueType.Greeting];
                    FarewellSelected = DialogueTypeList[Index].Selection[DialogueType.Farewell];
                    IdleSelected = DialogueTypeList[Index].Selection[DialogueType.Idle];
                    DialogueSelected = DialogueTypeList[Index].Selection[DialogueType.Dialogue];
                    GenericSceneSelected = DialogueTypeList[Index].Selection[DialogueType.GenericScene];
                    QuestSceneSelected = DialogueTypeList[Index].Selection[DialogueType.QuestScene];
                }
            });

        this.WhenAnyValue(v => v.QuestFormKey)
            .Subscribe(_ => {
                ValidQuest = QuestFormKey != FormKey.Null;
                DialogueImplementer = new DialogueImplementer(QuestFormKey);
            });
        
        this.WhenAnyValue(v => v.SpeakerFormKey)
            .Subscribe(_ => {
                ValidSpeaker = SpeakerFormKey != FormKey.Null;
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].Speaker = SpeakerFormKey;
                if (SpeakerFormKey != FormKey.Null && SpeakerFavourites.All(s => s.FormKey != SpeakerFormKey)) {
                    SpeakerFavourites.Add(new Speaker(SpeakerFormKey));
                }
            });

        this.WhenAnyValue(v => v.GreetingSelected)
            .Subscribe(_ => {
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].Selection[DialogueType.Greeting] = GreetingSelected;
            });
        
        this.WhenAnyValue(v => v.FarewellSelected)
            .Subscribe(_ => {
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].Selection[DialogueType.Farewell] = FarewellSelected;
            });
        
        this.WhenAnyValue(v => v.IdleSelected)
            .Subscribe(_ => {
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].Selection[DialogueType.Idle] = IdleSelected;
            });
        
        this.WhenAnyValue(v => v.DialogueSelected)
            .Subscribe(_ => {
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].Selection[DialogueType.Dialogue] = DialogueSelected;
            });
        
        this.WhenAnyValue(v => v.GenericSceneSelected)
            .Subscribe(_ => {
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].Selection[DialogueType.GenericScene] = GenericSceneSelected;
            });
        
        this.WhenAnyValue(v => v.QuestSceneSelected)
            .Subscribe(_ => {
                if (DialogueTypeList.Count > Index) DialogueTypeList[Index].Selection[DialogueType.QuestScene] = QuestSceneSelected;
            });
    }

    public void Init(DocumentParser parser) {
        DocumentParser = parser;
        Index = 1;
        Index = 0;
        
        App.DialogueVM.SavedSession = false;
        
        //Use new implementer when quest changed
        if (DialogueImplementer.Quest.FormKey != QuestFormKey) DialogueImplementer = new DialogueImplementer(QuestFormKey);
        
        //Clear dialogue data
        DialogueTypeList.Clear();
        for (var i = 0; i <= DocumentParser.LastIndex; i++) App.DialogueVM.DialogueTypeList.Add(new DialogueSelection());
        
        //Set buttons to unchecked
        GreetingSelected = FarewellSelected = IdleSelected = DialogueSelected = GenericSceneSelected = QuestSceneSelected = false;
    }

    public void RefreshPreview(bool forward) {
        var preview = string.Empty;
        var tries = 0;
        while (string.IsNullOrWhiteSpace(preview) && tries < 10) {
            preview = App.DialogueVM.DocumentParser.PreviewCurrent();
            if (string.IsNullOrEmpty(preview)) {
                if (forward) {
                    App.DialogueVM.DocumentParser.Next();   
                } else {
                    App.DialogueVM.DocumentParser.Previous();
                }
            } else {
                PreviewText = preview;
            }
            tries++;
        }
    }
    
    public void OpenOutput() {
        if (!Directory.Exists(DialogueFactory.OutputFolder)) return;

        using var process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "explorer",
                Arguments = $"\"{DialogueFactory.OutputFolder}\""
            }
        };
        process.Start();
    }
}
