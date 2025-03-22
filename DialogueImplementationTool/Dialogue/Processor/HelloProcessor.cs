using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class HelloProcessor(IDialogueContext context) : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype) =>
        subtype is DialogTopic.SubtypeEnum.Hello;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        var voiceTypeOrList = GenericMetaData.GetVoiceType(topicInfo.MetaData);

        switch (description) {
            case "Neutral/Base (0)":
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.EqualTo, comparisonValue: 0);

                break;
            case "Negative (-1 to -4)":
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.LessThanOrEqualTo, comparisonValue: -1);

                break;
            case "Positive (1 to 3)":
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.GreaterThanOrEqualTo, comparisonValue: 1);
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.LessThanOrEqualTo, comparisonValue: 3);

                break;
            case "Lover (4)":
                yield return new GetRelationshipRankConditionData {
                    TargetNpc = { Link = { FormKey = Skyrim.PlayerRef.FormKey } }
                }.ToConditionFloat(compareOperator: CompareOperator.EqualTo, comparisonValue: 4);

                break;
            case "Eclipse happens":
                yield return new GetGlobalValueConditionData {
                    Global = { Link = { FormKey = Dawnguard.Global.DLC1EclipseActive.FormKey } }
                }.ToConditionFloat(comparisonValue: 1);

                break;
            case "Player is a vampire (non-combat)":
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.Reference,
                    Reference = Skyrim.PlayerRef,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.Vampire.FormKey } }
                }.ToConditionFloat();

                break;
            case "Player is an untransformed werewolf (non-combat)":
                yield return new GetGlobalValueConditionData {
                    Global = { Link = { FormKey = Skyrim.Global.PlayerIsWerewolf.FormKey } }
                }.ToConditionFloat(comparisonValue: 1);

                break;
            case "Player wakes up a sleeping NPC":
                yield return new GetSleepingConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.GreaterThanOrEqualTo,
                    comparisonValue: 3
                );

                break;
            case "Player is seen sneaking":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData,
                    new WICommentQuestFactory(
                        context,
                        "Sneaking",
                        "Player is seen sneaking",
                        [
                            new ConditionGlobal {
                                Data = new GetRandomPercentConditionData(),
                                CompareOperator = CompareOperator.LessThanOrEqualTo,
                                ComparisonValue = Skyrim.Global.WICommentChanceSneaking,
                            },
                            new IsSneakingConditionData {
                                RunOnType = Condition.RunOnType.Reference,
                                Reference = Skyrim.PlayerRef
                            }.ToConditionFloat(),
                        ]));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceTypeOrList.FormKey } },
                }.ToConditionFloat();

                break;
            case "Player is naked":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData,
                    new WICommentQuestFactory(
                        context,
                        "Naked",
                        "Player is seen naked",
                        [
                            new ConditionGlobal {
                                Data = new GetRandomPercentConditionData(),
                                CompareOperator = CompareOperator.LessThanOrEqualTo,
                                ComparisonValue = Skyrim.Global.WICommentChanceNaked,
                            },
                            new WornHasKeywordConditionData {
                                RunOnType = Condition.RunOnType.Target,
                                Keyword = { Link = { FormKey = Skyrim.Keyword.ArmorCuirass.FormKey } },
                            }.ToConditionFloat(comparisonValue: 0),
                            new WornHasKeywordConditionData {
                                RunOnType = Condition.RunOnType.Target,
                                Keyword = { Link = { FormKey = Skyrim.Keyword.ClothingBody.FormKey } },
                            }.ToConditionFloat(comparisonValue: 0),
                        ]));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceTypeOrList.FormKey } },
                }.ToConditionFloat();

                break;
            case "Player is sick":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData,
                    new WICommentQuestFactory(
                        context,
                        "Diseased",
                        "Player is seen sick",
                        [
                            new ConditionGlobal {
                                Data = new GetRandomPercentConditionData(),
                                CompareOperator = CompareOperator.LessThanOrEqualTo,
                                ComparisonValue = Skyrim.Global.WICommentChanceDiseased,
                            },
                            new GetDiseaseConditionData {
                                RunOnType = Condition.RunOnType.Reference,
                                Reference = Skyrim.PlayerRef,
                            }.ToConditionFloat(),
                        ]));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceTypeOrList.FormKey } },
                }.ToConditionFloat();

                break;
            case "Player is seen with an active flame spell":
                yield return WICommentQuestForSpellType(Skyrim.Keyword.MagicDamageFire.FormKey);

                break;
            case "Player is seen with an active frost spell":
                yield return WICommentQuestForSpellType(Skyrim.Keyword.MagicDamageFrost.FormKey);

                break;
            case "Player is seen with an active shock spell":
                yield return WICommentQuestForSpellType(Skyrim.Keyword.MagicDamageShock.FormKey);

                break;
            case "Player is seen with an active water spell":
                var waterSpell = FormKey.Factory("00ABE1:BSAssets.esm");
                yield return WICommentQuestForSpellType(waterSpell);

                break;
            case "Player is seen with an active earth spell":
                var earthSpell = FormKey.Factory("00AB75:BSAssets.esm");
                yield return WICommentQuestForSpellType(earthSpell);

                break;
            case "Player is seen with an active wind spell":
                var windSpell = FormKey.Factory("00337F:BSAssets.esm");
                yield return WICommentQuestForSpellType(windSpell);

                break;
            case "Player casts a dangerous spell":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData,
                    new WICommentQuestFactory(
                        context,
                        "MagicDangerous",
                        "Player is seen with dangerous on going magic effect on",
                        [
                            new ConditionGlobal {
                                Data = new GetRandomPercentConditionData(),
                                CompareOperator = CompareOperator.LessThanOrEqualTo,
                                ComparisonValue = Skyrim.Global.WICommentChanceMagicDangerous,
                            },
                            new HasMagicEffectKeywordConditionData {
                                RunOnType = Condition.RunOnType.Target,
                                Keyword = { Link = { FormKey = Skyrim.Keyword.WISpellDangerous.FormKey } },
                            }.ToConditionFloat(or: true),
                            new HasMagicEffectKeywordConditionData {
                                RunOnType = Condition.RunOnType.Target,
                                Keyword = { Link = { FormKey = Skyrim.Keyword.MagicCloak.FormKey } },
                            }.ToConditionFloat(),
                        ]));

                yield return new GetIsVoiceTypeConditionData {
                    VoiceTypeOrList = { Link = { FormKey = voiceTypeOrList.FormKey } },
                }.ToConditionFloat();

                break;
        }

        Condition WICommentQuestForSpellType(FormKey spellKeyword) {
            topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
            GenericMetaData.SetGenericQuestFactory(topicInfo.MetaData,
                new WICommentQuestFactory(
                    context,
                    "MagicFlames",
                    "Player is seen with magic flames equipped",
                    [
                        new ConditionGlobal {
                            Data = new GetRandomPercentConditionData(),
                            CompareOperator = CompareOperator.LessThanOrEqualTo,
                            ComparisonValue = Skyrim.Global.WICommentChanceMagicFlames,
                        },
                        new IsWeaponMagicOutConditionData {
                            RunOnType = Condition.RunOnType.Target,
                        }.ToConditionFloat(),
                        new SpellHasKeywordConditionData {
                            SpellSource = CastSource.Left,
                            Keyword = { Link = { FormKey = spellKeyword } }
                        }.ToConditionFloat(or: true),
                        new SpellHasKeywordConditionData {
                            SpellSource = CastSource.Right,
                            Keyword = { Link = { FormKey = spellKeyword } }
                        }.ToConditionFloat(),
                    ]));

            return new GetIsVoiceTypeConditionData {
                VoiceTypeOrList = { Link = { FormKey = voiceTypeOrList.FormKey } },
            }.ToConditionFloat();
        }
    }
}
