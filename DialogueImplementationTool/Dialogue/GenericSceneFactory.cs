using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Skyrim.Internals;
using Noggog;
using Condition = Mutagen.Bethesda.Skyrim.Condition;
namespace DialogueImplementationTool.Dialogue;

public class GenericSceneFactory(IDialogueContext context) : SceneFactory(context) {
    public override BaseDialogueFactory SpecifyType(List<DialogueTopic> topics) {
        // 3x3 scenes have a prompt set on their topic infos
        if (topics.Exists(t => t.TopicInfos.Exists(x => x.Prompt.FullText != string.Empty))) {
            return new GenericScene3x3Factory(Context);
        }

        return this;
    }

    public override void PreProcessSpeakers() {
        //Make sure there are at least two speakers
        if (AliasSpeakers.Count < 2) MessageBox.Show("Error, there must be at least 2 NPCs");
    }

    protected override (Scene? Scene, IQuest? QuestForDialogue) GetCurrentScene() {
        //Assign alias indices
        for (var i = 0; i < AliasSpeakers.Count; i++) {
            AliasSpeakers[i].AliasIndex = AliasSpeakers.Count + i;
        }

        //Add quest
        var npcNames = AliasSpeakers
            .Select(a => Context.LinkCache.Resolve<INpcGetter>(a.FormKey).GetName())
            .Order()
            .ToList();

        var baseName = $"{Context.Quest.EditorID}Scene{string.Join(string.Empty, npcNames)}";
        var questEditorId = Naming.GetFirstFreeIndex(
            i => baseName + i,
            name => !Context.LinkCache.TryResolve<IQuestGetter>(name, out _),
            1);

        var aliases = new ExtendedList<QuestAlias>();

        var getIsIdConditions = AliasSpeakers
            .Select(a => {
                var data = new GetIsIDConditionData { Object = { Link = { FormKey = a.FormKey } } };
                return GetFormKeyCondition(data, or: true);
            })
            .ToExtendedList();

        // Create event aliases for the first two speakers
        aliases.Add(CreateEventAlias(0, "Actor 1", [0x52, 0x31, 0x0, 0x0], getIsIdConditions));
        aliases.Add(CreateEventAlias(1, "Actor 2", [0x52, 0x32, 0x0, 0x0], getIsIdConditions));

        // Add remaining base aliases for additional speakers based on distance condition
        if (AliasSpeakers.Count > 2) {
            for (var i = 0; i < AliasSpeakers.Count; i++) {
                if (i < 2) continue;

                aliases.Add(CreateFakeEventAlias(i));
            }
        }

        // Create unique npc fill type aliases
        aliases.AddRange(AliasSpeakers.Select(CreateAlias));

        var sceneQuest = new Quest(Context.GetNextFormKey(), Context.Release) {
            EditorID = questEditorId,
            Priority = 10,
            Type = Quest.TypeEnum.None,
            Filter = Context.Quest.Filter,
            Name = $"{Context.Quest.Name?.String} Scene {string.Join(" ", npcNames)} {questEditorId[^1]}",
            Event = RecordTypes.ADIA,
            Aliases = aliases,
        };
        Context.AddQuest(sceneQuest);

        //Add scene
        var scene = AddScene($"{questEditorId}Scene", sceneQuest.FormKey);
        scene.Flags = new Scene.Flag();
        scene.Flags |= Scene.Flag.BeginOnQuestStart | Scene.Flag.StopQuestOnEnd | Scene.Flag.Interruptable;
        Context.AddScene(scene);

        return (scene, sceneQuest);

        QuestAlias CreateEventAlias(uint id, string name, byte[] eventData, ExtendedList<Condition> conditions) {
            return new QuestAlias {
                ID = id,
                Name = name,
                FindMatchingRefFromEvent = new FindMatchingRefFromEvent {
                    FromEvent = RecordTypes.ADIA,
                    EventData = eventData,
                },
                Conditions = conditions,
                Flags = QuestAlias.Flag.AllowReserved,
                VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
            };
        }

        QuestAlias CreateFakeEventAlias(int aliasIndex) => new() {
            ID = (uint) aliasIndex,
            Type = QuestAlias.TypeEnum.Reference,
            Name = "Actor " + (aliasIndex + 1),
            Flags = QuestAlias.Flag.AllowReserved,
            Conditions = [
                new ConditionFloat {
                    Data = new GetDistanceConditionData {
                        UseAliases = true,
                        Target = {
                            Link = {
                                FormKey = AliasSpeakers[aliasIndex].FormKey
                            }
                        },
                    }
                }
            ],
        };

        QuestAlias CreateAlias(AliasSpeaker aliasSpeaker) {
            const QuestAlias.Flag genericSceneAliasFlags =
                QuestAlias.Flag.AllowReserved | QuestAlias.Flag.AllowReuseInQuest;

            return new QuestAlias {
                ID = Convert.ToUInt32(aliasSpeaker.AliasIndex),
                Name = aliasSpeaker.NameNoSpaces,
                UniqueActor = new FormLinkNullable<INpcGetter>(aliasSpeaker.FormKey),
                VoiceTypes = new FormLinkNullable<IAliasVoiceTypeGetter>(FormKey.Null),
                Flags = genericSceneAliasFlags
            };
        }
    }
}
