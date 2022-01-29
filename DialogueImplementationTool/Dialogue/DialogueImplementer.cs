using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using DialogueImplementationTool.UI;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue; 

public class DialogueImplementer {
    private static readonly IGameEnvironmentState<ISkyrimMod, ISkyrimModGetter> Environment = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
    
    private readonly SkyrimMod _mod = new(new ModKey("DialogueOutput", ModType.Plugin), SkyrimRelease.SkyrimSE);
    private IQuestGetter _quest = new Quest(FormKey.Null, SkyrimRelease.SkyrimSE);

    public DialogueImplementer() {
        ForceQuestSelection();
    }

    private void ForceQuestSelection() {
        var winningOverrides = Environment.LoadOrder.PriorityOrder.Quest().WinningOverrides().ToList();
        var questSelectionWindow = new QuestSelectionWindow(winningOverrides);
        questSelectionWindow.OnQuestSelected += QuestSelectionWindowOnOnQuestSelected;

        var success = questSelectionWindow.ShowDialog();
        while (success is null or false) {
            MessageBox.Show("Selection not valid!");
            
            questSelectionWindow = new QuestSelectionWindow(winningOverrides);
            questSelectionWindow.OnQuestSelected += QuestSelectionWindowOnOnQuestSelected;
            
            success = questSelectionWindow.ShowDialog();
        }
    }
    
    private void QuestSelectionWindowOnOnQuestSelected(object sender, RoutedEventArgs e) {
        _quest = (IQuestGetter) sender;

        //Get master references setup
        var questContext = Environment.LinkCache.ResolveContext<IQuest, IQuestGetter>(_quest.FormKey);
        questContext.GetOrAddAsOverride(_mod);
    }

    public void AddGreeting(List<DialogueTopic> topics) {
        foreach (var dialogueTopic in topics) {
            
        }
    }
    
    public void AddFarewell(List<DialogueTopic> topics) {
        foreach (var dialogueTopic in topics) {
            
        }
    }
    
    public void AddDialogue(List<DialogueTopic> topics) {
        foreach (var dialogueTopic in topics) {
            
        }
    }
    
    public void AddIdle(List<DialogueTopic> topics) {
        foreach (var dialogueTopic in topics) {
            
        }
    }
    
    public void AddScene(List<DialogueTopic> topics) {
        foreach (var dialogueTopic in topics) {
            
        }
    }
    
    public void AddQuestScene(List<DialogueTopic> topics) {
        foreach (var dialogueTopic in topics) {
            
        }
    }

    public void Save() {
        var fileInfo = new FileInfo($"{AppDomain.CurrentDomain.BaseDirectory}\\Output\\{_mod.ModKey.FileName}");
        if (!fileInfo.Exists) fileInfo.Directory?.Create();
        _mod.WriteToBinaryParallel(fileInfo.FullName);
    }
}
