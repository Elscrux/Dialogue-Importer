using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mutagen.Bethesda.Skyrim;
using MutagenLibrary.WPF.CustomElements;
using MutagenLibrary.WPF.CustomElements.Views;
namespace DialogueImplementationTool.UI; 

public partial class QuestSelectionWindow {
    public ObservableCollection<RecordItem> Quests { get; }
    public event RoutedEventHandler? OnQuestSelected;
    
    public QuestSelectionWindow(IEnumerable<IQuestGetter> quests) {
        InitializeComponent();
        
        Quests = new ObservableCollection<RecordItem>(quests.Select(q => new RecordItem(q)).ToList());
        
        var select = new CommandBinding { Command = RecordView.GenericCommand };
        select.Executed += SelectQuest;
        select.CanExecute += RecordView.CanExecuteOneItemSelected;
        RecordView.AddCommands(new List<CustomCommand> {
            new("Select", select)
        });
    }
    
    private void SelectQuest(object sender, ExecutedRoutedEventArgs e) {
        foreach (RecordItem? record in RecordView.GetSelectedItems()) {
            if (record == null) continue;

            OnQuestSelected?.Invoke(record.Record, new RoutedEventArgs());
            DialogResult = true;
            Close();
        }
    }
}