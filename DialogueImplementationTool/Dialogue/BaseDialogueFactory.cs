using System;
using System.Collections.Generic;
using System.Linq;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Dialogue.Processor;
using DialogueImplementationTool.Dialogue.Speaker;
using DialogueImplementationTool.Extension;
using DialogueImplementationTool.Parser;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
namespace DialogueImplementationTool.Dialogue;

public abstract class BaseDialogueFactory(IDialogueContext context) {
    protected readonly IDialogueContext Context = context;

    public abstract void GenerateDialogue(List<DialogueTopic> topics);

    public static BaseDialogueFactory GetBaseFactory(DialogueType type, IDialogueContext context) {
        return type switch {
            DialogueType.Dialogue => new DialogueFactory(context),
            DialogueType.Greeting => new GreetingFactory(context),
            DialogueType.Farewell => new FarewellFactory(context),
            DialogueType.Idle => new IdleFactory(context),
            DialogueType.GenericScene => new GenericSceneFactory(context),
            DialogueType.QuestScene => new QuestSceneFactory(context),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    public virtual BaseDialogueFactory SpecifyType(List<DialogueTopic> topics) => this;

    public virtual IDialogueProcessor ConfigureProcessor(DialogueProcessor dialogueProcessor) => dialogueProcessor;

    public void Create(GeneratedDialogue generatedDialogue) {
        if (generatedDialogue.Topics.Count == 0) return;

        GenerateDialogue(generatedDialogue.Topics);
    }

    protected static Condition GetFormKeyCondition(
        ConditionData data,
        float comparisonValue = 1,
        bool or = false) {
        var condition = new ConditionFloat {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = comparisonValue,
            Data = data,
        };

        if (or) condition.Flags = Condition.Flag.OR;

        return condition;
    }

    protected ExtendedList<DialogResponses> GetTopicInfos(IQuestGetter quest, DialogueTopic topic) {
        var responses = topic.TopicInfos.Select(info => GetResponses(quest, info)).ToExtendedList();
        for (var i = 1; i < responses.Count; i++) {
            responses[i].PreviousDialog.SetTo(responses[i - 1].FormKey);
        }
        return responses;
    }

    public DialogResponses GetResponses(IQuestGetter quest, DialogueTopicInfo topicInfo, FormKey? previousDialogue = null) {
        var previousDialog = new FormLinkNullable<IDialogResponsesGetter>(previousDialogue ?? FormKey.Null);

        var flags = new DialogResponseFlags();

        // Handle flags
        if (topicInfo.SayOnce) flags.Flags |= DialogResponses.Flag.SayOnce;
        if (topicInfo.Goodbye) flags.Flags |= DialogResponses.Flag.Goodbye;
        if (topicInfo.InvisibleContinue) flags.Flags |= DialogResponses.Flag.InvisibleContinue;
        if (topicInfo.Random) flags.Flags |= DialogResponses.Flag.Random;
        if (topicInfo.ResetHours is > 0 and <= 24) flags.ResetHours = topicInfo.ResetHours;

        // Handle shared info
        if (topicInfo.SharedInfo is not null) {
            // Empty line shared info
            if (topicInfo.SharedInfo.ResponseDataTopicInfo.Responses is [] or [{ Text: "" }]) {
                var emptyLine = Context.SelectRecord<DialogResponses, IDialogResponsesGetter>("Empty Line Shared Info");
                return new DialogResponses(Context.GetNextFormKey(), Context.Release) {
                    ResponseData = new FormLinkNullable<IDialogResponsesGetter>(emptyLine.FormKey),
                    Conditions = GetConditions(topicInfo),
                    Flags = flags,
                    PreviousDialog = previousDialog,
                };
            }

            var dialogResponses = topicInfo.SharedInfo.GetResponseData(quest, Context, TopicInfos, GetConditions);
            dialogResponses.Conditions.AddRange(topicInfo.ExtraConditions);
            dialogResponses.PreviousDialog = previousDialog;
            dialogResponses.Flags = flags;

            return dialogResponses;
        }

        // Handle responses
        var responses = new DialogResponses(Context.GetNextFormKey(), Context.Release) {
            Responses = TopicInfos(topicInfo).ToExtendedList(),
            Prompt = topicInfo.Prompt.FullText.IsNullOrWhitespace() ? null : topicInfo.Prompt.FullText,
            Conditions = GetConditions(topicInfo),
            FavorLevel = FavorLevel.None,
            Flags = flags,
            PreviousDialog = previousDialog,
        };

        // Handle scripts
        BuildFragment(topicInfo.Script, responses);

        // Report remaining notes
        if (topicInfo.Prompt.Notes().Any()) {
            Context.Issues.Add($"{topicInfo.Speaker.NameNoSpaces}: Prompt \"{topicInfo.Prompt.FullText}\" has notes.");
        }
        foreach (var response in topicInfo.Responses.Where(response => response.Notes().Any())) {
            Context.Issues.Add($"{topicInfo.Speaker.NameNoSpaces}: Response \"{response.FullResponse}\" has notes.");
        }
        foreach (var note in topicInfo.Prompt.Notes().Concat(topicInfo.Responses.SelectMany(r => r.Notes()))) {
            Context.Issues.Add($"Note: {note.Text}");
        }

        return responses;

        static IEnumerable<DialogResponse> TopicInfos(DialogueTopicInfo info) {
            return info.Responses.Select((line, i) => new DialogResponse {
                Text = line.FullResponse,
                ScriptNotes = line.ScriptNote,
                ResponseNumber = (byte) (i + 1), //Starts with 1
                Flags = DialogResponse.Flag.UseEmotionAnimation,
                Emotion = line.Emotion,
                EmotionValue = line.EmotionValue,
                SpeakerIdleAnimation = new FormLinkNullable<IIdleAnimationGetter>(line.SpeakerIdle),
            });
        }
    }

    private void BuildFragment(DialogueScript script, DialogResponses responses) {
        var hasStart = script.StartScriptLines.Count > 0;
        var hasEnd = script.EndScriptLines.Count > 0;
        if (!hasStart && !hasEnd) return;

        var propertyLines = script.Properties
            .DistinctBy(x => x.ScriptProperty.Name)
            .Select(property => $"{property.ScriptName} Property {property.ScriptProperty.Name} Auto")
            .ToList();

        var middlePart = string.Empty;
        if (hasStart) middlePart += GetFragmentCode(script.StartScriptLines, 0) + "\n";
        if (hasEnd) middlePart += GetFragmentCode(script.EndScriptLines, 1) + "\n";

        var nameStart = Context.Prefix.IsNullOrEmpty() ? string.Empty : Context.Prefix + '_';
        var scriptName = $"{nameStart}TIF__{responses.FormKey.ToFormID(Context.LinkCache)}";
        var nextFragment = hasEnd ? 2 : 1;
        var scriptText = $"""
            ;BEGIN FRAGMENT CODE - Do not edit anything between this and the end comment
            ;NEXT FRAGMENT INDEX {nextFragment}
            Scriptname {scriptName} Extends TopicInfo Hidden

            {middlePart}

            ;END FRAGMENT CODE - Do not edit anything between this and the begin comment

            {string.Join("\r\n", propertyLines)}
            """;

        Context.Scripts.Add(scriptName, scriptText);

        var scriptFragments = new ScriptFragments { FileName = scriptName };
        if (hasStart) {
            scriptFragments.OnBegin = new ScriptFragment {
                ExtraBindDataVersion = 2,
                ScriptName = scriptName,
                FragmentName = "Fragment_0",
            };
        }

        if (hasEnd) {
            scriptFragments.OnEnd = new ScriptFragment {
                ExtraBindDataVersion = 2,
                ScriptName = scriptName,
                FragmentName = "Fragment_1",
            };
        }

        responses.VirtualMachineAdapter = new DialogResponsesAdapter {
            Scripts = [
                new ScriptEntry {
                    Name = scriptName,
                    Flags = ScriptEntry.Flag.Local,
                    Properties = script.Properties
                        .Select(x => x.ScriptProperty)
                        .DistinctBy(x => x.Name)
                        .ToExtendedList()
                }
            ],
            ScriptFragments = scriptFragments,
        };

        string GetFragmentCode(IEnumerable<string> lines, int index) => $"""
            ;BEGIN FRAGMENT Fragment_{index}
            Function Fragment_{index}(ObjectReference akSpeakerRef)
            Actor akSpeaker = akSpeakerRef as Actor
            ;BEGIN CODE
            {string.Join("\r\n", lines)}
            ;END CODE
            EndFunction
            ;END FRAGMENT
            """;
    }

    public ExtendedList<Condition> GetConditions(DialogueTopicInfo topicInfo) {
        var list = new ExtendedList<Condition>();

        if (topicInfo.Speaker is AliasSpeaker aliasSpeaker) {
            list.Add(new GetIsAliasRefConditionData {
                ReferenceAliasIndex = aliasSpeaker.AliasIndex,
            }.ToConditionFloat());
        } else if (Context.LinkCache.TryResolve<INpcGetter>(topicInfo.Speaker.FormLink.FormKey, out var npc)) {
            list.Add(new GetIsIDConditionData {
                Object = {
                    Link = { FormKey = npc.FormKey }
                }
            }.ToConditionFloat());
        } else if (Context.LinkCache.TryResolve<IFactionGetter>(topicInfo.Speaker.FormLink.FormKey, out var faction)) {
            list.Add(new GetInFactionConditionData {
                Faction = { Link = { FormKey = faction.FormKey } }
            }.ToConditionFloat());
        } else if (Context.LinkCache.TryResolve<IVoiceTypeGetter>(topicInfo.Speaker.FormLink.FormKey, out var voiceType)) {
            list.Add(new GetIsVoiceTypeConditionData {
                VoiceTypeOrList = { Link = { FormKey = voiceType.FormKey } }
            }.ToConditionFloat());
        } else if (Context.LinkCache.TryResolve<IFormListGetter>(topicInfo.Speaker.FormLink.FormKey, out var formList)) {
            list.Add(new GetIsVoiceTypeConditionData {
                VoiceTypeOrList = { Link = { FormKey = formList.FormKey } }
            }.ToConditionFloat());
        } else if (Context.LinkCache.TryResolve<ITalkingActivatorGetter>(topicInfo.Speaker.FormLink.FormKey, out var talkingActivator)) {
            list.Add(new GetIsIDConditionData {
                Object = { Link = { FormKey = talkingActivator.FormKey } }
            }.ToConditionFloat());
        }

        list.AddRange(topicInfo.ExtraConditions);

        return list;
    }
}
