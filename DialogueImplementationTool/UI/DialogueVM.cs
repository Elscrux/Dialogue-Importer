using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public ObservableCollection<SpeakerFavourite> SpeakerFavourites { get; } = new();

    public bool SavedSession;

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

    public DialogueVM() {
        SetSpeaker = ReactiveCommand.Create((FormKey formKey) => SpeakerFormKey = formKey);
        
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
                    var record = LinkCache.Resolve<INpcGetter>(SpeakerFormKey);
                    SpeakerFavourites.Add(new SpeakerFavourite(SpeakerFormKey, record.EditorID));
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
    
    public void Save() {
        App.DialogueVM.DialogueImplementer.ImplementDialogue(App.DialogueVM.DocumentParser.GetDialogue());
        DialogueFactory.Save();
        App.DialogueVM.SavedSession = true;
    }
}
