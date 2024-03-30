using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

public sealed class DialogueVM : ViewModel {
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

	[Reactive] public string PythonDllPath { get; set; }
	public EmotionClassifier? EmotionClassifier { get; private set; }

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
	public IEnumerable<Type> SpeakerTypes { get; } = new List<Type> { typeof(INpcGetter), typeof(IFactionGetter), typeof(IVoiceTypeGetter), typeof(IFormListGetter) };
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
	[Reactive] public string Title { get; set; } = string.Empty;
	[Reactive] public bool UseGetIsAliasRef { get; set; }

	public DialogueVM() {
		Task.Run(() => {
			foreach (var speakerType in SpeakerTypes) {
				LinkCache.Warmup(speakerType);
			}
		});

		TrySetPythonPath();

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
			DialogueImplementer.ImplementDialogue(DocumentParser.GetDialogue());
			DialogueFactory.Save();
			SavedSession = true;
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
					if (DialogueTypeList[Index].Speaker == FormKey.Null) {
						// Keep current speaker for fresh dialogue and set in list
						DialogueTypeList[Index].Speaker = SpeakerFormKey;
						DialogueTypeList[Index].UseGetIsAliasRef = UseGetIsAliasRef;
					} else {
						// Load speaker from list
						SpeakerFormKey = DialogueTypeList[Index].Speaker;
						UseGetIsAliasRef = DialogueTypeList[Index].UseGetIsAliasRef;
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

		this.WhenAnyValue(v => v.UseGetIsAliasRef)
			.Subscribe(x => {
				if (DialogueTypeList.Count > Index) DialogueTypeList[Index].UseGetIsAliasRef = x;
			});
	}

	private void TrySetPythonPath() {
		var paths = Environment.GetEnvironmentVariable("PATH");
		if (paths is null) return;

		foreach (var path in paths.Split(';')) {
			if (!path.Contains("python", StringComparison.OrdinalIgnoreCase)) continue;
			if (!Directory.Exists(path)) continue;

			var filePath = Directory
				.EnumerateFiles(path, "python3*.dll", SearchOption.TopDirectoryOnly)
				// Don't use python3.dll
				.Where(x => !x.Contains("python3.dll"))
				.FirstOrDefault(File.Exists);
			if (filePath is null) continue;

			PythonDllPath = filePath;
			break;
		}
	}

	public void RefreshPython() {
		if (EmotionClassifier?.PythonDllPath == PythonDllPath) return;

		try {
			EmotionClassifier?.Dispose();
			EmotionClassifier = null;
			EmotionClassifier = new EmotionClassifier(PythonDllPath);
		} catch (Exception e) {
			Console.WriteLine($"Failed to load emotion classifier: {e.Message}");
		}
	}

	public void Init(DocumentParser parser) {
		RefreshPython();
		DocumentParser = parser;
		Index = 1;
		Index = 0;

		Title = Path.GetFileName(parser.FilePath);
		SavedSession = false;

		//Use new implementer when quest changed
		if (DialogueImplementer.Quest.FormKey != QuestFormKey) DialogueImplementer = new DialogueImplementer(QuestFormKey);

		//Clear dialogue data
		DialogueTypeList.Clear();
		for (var i = 0; i <= DocumentParser.LastIndex; i++) DialogueTypeList.Add(new DialogueSelection());

		//Set buttons to unchecked
		GreetingSelected = FarewellSelected = IdleSelected = DialogueSelected = GenericSceneSelected = QuestSceneSelected = false;
	}

	public void RefreshPreview(bool forward) {
		var preview = string.Empty;
		var tries = 0;
		while (string.IsNullOrWhiteSpace(preview) && tries < 10) {
			preview = DocumentParser.PreviewCurrent();
			if (string.IsNullOrEmpty(preview)) {
				if (forward) {
					DocumentParser.Next();
				} else {
					DocumentParser.Previous();
				}
			} else {
				PreviewText = preview;
			}
			tries++;
		}
	}

	public void OpenOutput() {
		if (!Directory.Exists(DialogueFactory.OutputFolder)) return;

		using var process = new Process();
		process.StartInfo = new ProcessStartInfo {
			FileName = "explorer",
			Arguments = $"\"{DialogueFactory.OutputFolder}\""
		};
		process.Start();
	}

	public override void Dispose() {
		Dispose(true);
	}

	private void Dispose(bool disposing) {
		if (disposing) {
			EmotionClassifier?.Dispose();
		}
	}
}
