using System.Collections.Generic;
using DialogueImplementationTool.Dialogue.Model;
using DialogueImplementationTool.Extension;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Dialogue.Processor;

public sealed class NoticeCorpseProcessor : DialogueTypeProcessor {
    protected override bool IsApplicable(DialogTopic.SubtypeEnum subtype)
        => subtype is DialogTopic.SubtypeEnum.NoticeCorpse;

    protected override IEnumerable<Condition> GetConditions(string description, DialogueTopicInfo topicInfo) {
        switch (description) {
            case "NPC sees a corpse":
                yield return new GetFactionRelationConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.NotEqualTo,
                    comparisonValue: 1
                );
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Faction = { Link = { FormKey = Skyrim.Faction.BanditFaction.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Faction = { Link = { FormKey = Skyrim.Faction.NecromancerFaction.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Faction = { Link = { FormKey = Skyrim.Faction.WarlockFaction.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);
                yield return new ConditionFloat {
                    Data = new GetShouldAttackConditionData(),
                    CompareOperator = CompareOperator.EqualTo,
                    ComparisonValue = 0
                };
                yield return new ConditionFloat {
                    Data = new GetShouldAttackConditionData(),
                    CompareOperator = CompareOperator.EqualTo,
                    Flags = Condition.Flag.SwapSubjectAndTarget,
                    ComparisonValue = 0
                };

                break;
            case "NPC sees a corpse of a friend":
                yield return new GetFactionRelationConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.NotEqualTo,
                    comparisonValue: 1
                );
                yield return new HasKeywordConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Keyword = { Link = { FormKey = Skyrim.Keyword.ActorTypeNPC.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Faction = { Link = { FormKey = Skyrim.Faction.BanditFaction.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Faction = { Link = { FormKey = Skyrim.Faction.NecromancerFaction.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);
                yield return new GetInFactionConditionData {
                    RunOnType = Condition.RunOnType.Target,
                    Faction = { Link = { FormKey = Skyrim.Faction.WarlockFaction.FormKey } }
                }.ToConditionFloat(comparisonValue: 0);
                yield return new ConditionFloat {
                    Data = new GetShouldAttackConditionData(),
                    CompareOperator = CompareOperator.EqualTo,
                    ComparisonValue = 0
                };
                yield return new ConditionFloat {
                    Data = new GetShouldAttackConditionData(),
                    CompareOperator = CompareOperator.EqualTo,
                    Flags = Condition.Flag.SwapSubjectAndTarget,
                    ComparisonValue = 0
                };
                yield return new GetRelationshipRankConditionData().ToConditionFloat(
                    compareOperator: CompareOperator.GreaterThan,
                    comparisonValue: 0
                );

                break;
        }
    }
}
