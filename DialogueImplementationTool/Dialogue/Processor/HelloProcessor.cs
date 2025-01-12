using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class HelloProcessor(IDialogueContext context) : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype) =>
        subtype is DialogTopic.SubtypeEnum.Hello;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
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
                }.ToConditionFloat(comparisonValue: 0);

                break;
            case "Waking up sleeping person":
                yield return new GetSleepingConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.GreaterThanOrEqualTo,
                    comparisonValue: 3
                );

                break;
            case "Player is seen sneaking":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                topicInfo.MetaData[GenericMetaData.GenericQuestFactory] = new WICommentQuestFactory(
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
                    ]);
                yield return NullCondition;

                break;
            case "Player is naked":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                topicInfo.MetaData[GenericMetaData.GenericQuestFactory] = new WICommentQuestFactory(
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
                    ]);
                yield return NullCondition;

                break;
            case "Player is sick":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                topicInfo.MetaData[GenericMetaData.GenericQuestFactory] = new WICommentQuestFactory(
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
                    ]);
                yield return NullCondition;

                break;
            case "Player is seen with an active flame spell":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                topicInfo.MetaData[GenericMetaData.GenericQuestFactory] = new WICommentQuestFactory(
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
                            Keyword = { Link = { FormKey = Skyrim.Keyword.MagicDamageFire.FormKey } }
                        }.ToConditionFloat(or: true),
                        new SpellHasKeywordConditionData {
                            SpellSource = CastSource.Right,
                            Keyword = { Link = { FormKey = Skyrim.Keyword.MagicDamageFire.FormKey } }
                        }.ToConditionFloat(),
                    ]);
                yield return NullCondition;

                break;
            case "Player casts a dangerous spell":
                topicInfo.Script.EndScriptLines.Add(WICommentQuestFactory.TopicCommentScript);
                topicInfo.MetaData[GenericMetaData.GenericQuestFactory] = new WICommentQuestFactory(
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
                    ]);
                yield return NullCondition;

                break;
        }
    }
}
