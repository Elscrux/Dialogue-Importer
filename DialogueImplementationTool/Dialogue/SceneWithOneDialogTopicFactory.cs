using DialogueImplementationTool.Dialogue.Model;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
namespace DialogueImplementationTool.Dialogue;

public sealed class SceneWithOneDialogTopicFactory(IDialogueContext context) : IGenericDialogueTopicFactory {
    public DialogTopic Create(IQuestGetter quest, DialogueTopicInfo topicInfo) {
        var scene = context.GetOrAddRecord<Scene, ISceneGetter>(quest.EditorID + "Scene",
            () => {
                var topic = new DialogTopic(context.GetNextFormKey(), context.Release) {
                    Quest = quest.ToNullableLink(),
                    Category = DialogTopic.CategoryEnum.Scene,
                    Subtype = DialogTopic.SubtypeEnum.Scene,
                    SubtypeName = RecordTypes.SCEN,
                };
                context.AddRecord(topic);
                return new Scene(context.GetNextFormKey(), context.Release) {
                    EditorID = quest.EditorID + "Scene",
                    Flags = Scene.Flag.BeginOnQuestStart | Scene.Flag.StopQuestOnEnd,
                    Phases = [
                        new ScenePhase {
                            // EditorWidth = 400,
                        }
                    ],
                    Actors = [new SceneActor { ID = 0 }],
                    Actions = [
                        new SceneAction {
                            Type = SceneAction.TypeEnum.Dialog,
                            ActorID = 0,
                            Index = 1,
                            Flags = SceneAction.Flag.HeadtrackPlayer,
                            StartPhase = 0,
                            EndPhase = 0,
                            Topic = topic.ToNullableLink(),
                            HeadtrackActorID = -1,
                            LoopingMax = 10.0f,
                            LoopingMin = 1.0f,
                        }
                    ],
                    Quest = quest.ToNullableLink(),
                    LastActionIndex = null,
                };
            });

        return context.GetOrAddOverride<DialogTopic, IDialogTopicGetter>(scene.Actions[0].Topic);
    }
}
