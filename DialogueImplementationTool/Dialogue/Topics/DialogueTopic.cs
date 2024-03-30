using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Responses;
using DynamicData;
namespace DialogueImplementationTool.Dialogue.Topics;

public sealed class DialogueTopic {
	private static readonly IEnumerable<IDialogueTopicPostProcessor> PreProcessors = new List<IDialogueTopicPostProcessor> {
		new SayOnceChecker(),
		new GoodbyeChecker(),
		new Trimmer(),
		new InvalidStringFixer(),
	};

	private static readonly IEnumerable<IDialogueTopicPostProcessor> PostProcessors = new List<IDialogueTopicPostProcessor> {
		new BackToOptionsLinker(),
		new EmotionClassifier(),
	};

	public SharedInfo? SharedInfo { get; set; }

	public ISpeaker Speaker { get; set; }

	public string Text { get; set; } = string.Empty;
	public List<DialogueResponse> Responses { get; } = [];
	public List<DialogueTopic> Links { get; } = [];
	public DialogueTopic? IncomingLink { get; set; }
	public bool SayOnce { get; set; }
	public bool Goodbye { get; set; }
	public bool InvisibleContinue { get; set; }
	public bool Blocking { get; set; }

	public void Build() {
		foreach (var preProcessor in PreProcessors) {
			preProcessor.Process(this);
		}
	}

	public void PostProcess() {
		foreach (var postProcessor in PostProcessors) {
			postProcessor.Process(this);
		}
	}

	public IEnumerable<DialogueTopic> EnumerateLinks() {
		yield return this;

		var returnedLinks = new HashSet<DialogueTopic>();

		var queue = new Queue<DialogueTopic>(Links);
		while (queue.Any()) {
			var dialogueTopic = queue.Dequeue();
			if (!returnedLinks.Add(dialogueTopic)) continue;

			foreach (var link in dialogueTopic.Links) {
				queue.Enqueue(link);
			}
			yield return dialogueTopic;
		}
	}

	/// <summary>
	/// Links to be played after this topic, linked with an invisible continue
	/// This handles all relinking of topics, flags, etc.
	/// </summary>
	/// <param name="nextTopic"></param>
	public void Append(DialogueTopic nextTopic) {
		// Handle invisible continue
		InvisibleContinue = true;

		// Handle Goodbye
		if (Goodbye) {
			nextTopic.Goodbye = true;
			Goodbye = false;
		}

		// Handle Links
		nextTopic.Links.Add(Links);
		foreach (var linkingTopic in Links) {
			linkingTopic.IncomingLink = nextTopic;
		}
		Links.Clear();
		Links.Add(nextTopic);
	}
}
