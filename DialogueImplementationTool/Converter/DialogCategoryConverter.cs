using System;
using Mutagen.Bethesda.Skyrim;
namespace DialogueImplementationTool.Converter;

public static class DialogCategoryConverter {
    public static (DialogTopic.CategoryEnum, DialogTopic.SubtypeEnum) Convert(string? text) {
        if (text is null) throw new InvalidOperationException("Category string not available");

        return text switch {
            "Greeting" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.Hello),
            "Farewell" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.Goodbye),
            "Idle" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.Idle),
            "GuardPursue" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.PursueIdleTopic),
            // Time to go subtype?
            "CollideActor" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.ActorCollideWithActor),
            "CollideItem" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.KnockOverObject),
            "LookAtLockedObject" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.LockedObject),
            "NoticeCorpse" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.NoticeCorpse),
            "ObserveCombat" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.ObserveCombat),
            "PlayerPickpocketWarn" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.PickpocketTopic),
            "AimBowAt" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.PlayerInIronSights),
            "PlayerinIronSights" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.PlayerInIronSights),
            "PlayerShout" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.PlayerShout),
            "PlayerShootBowNonCombat" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.ShootBow),
            "PlayerUseMeleeNonCombat" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.SwingMeleeWeapon),
            "PlayerGrabbingItem" => (DialogTopic.CategoryEnum.Misc, DialogTopic.SubtypeEnum.ZKeyObject),
            "CombatAcceptYield" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.AcceptYield),
            "CombatAttack" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Attack),
            "CombatBashingShield" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Bash),
            "CombatBleedingOut" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Bleedout),
            "CombatDying" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Death),
            "CombatFleeing" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Flee),
            "CombatBeingHit" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Hit),
            "CombatPowerAttack" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.PowerAttack),
            "CombatTaunt" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Taunt),
            "PlayerAssault" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Assault),
            "PlayerAssaultDontCare" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.AssaultNC),
            "PlayerMurder" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Murder),
            "PlayerMurderDontCare" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.MurderNC),
            "PlayerPickpocketCaught" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.PickpocketCombat),
            "PlayerPickpocketCaughtDontCare" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.PickpocketNC),
            "PlayerStealingCaught" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Steal),
            "PlayerStealingCaughtDontCare" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.StealFromNC),
            "PlayerTransformWerewolf" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.WerewolfTransformCrime),
            "Trespassing" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.Trespass),
            "TrespassingDontCare" => (DialogTopic.CategoryEnum.Combat, DialogTopic.SubtypeEnum.TrespassAgainstNC),
            "DetectionAlertIdle" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.AlertIdle),
            "DetectionLostIdle" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.LostIdle),
            "DetectionNormalToAlert" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.NormalToAlert),
            "DetectionAlertToCombat" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.AlertToCombat),
            "DetectionNormalToCombat" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.NormalToCombat),
            "DetectionAlertToNormal" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.AlertToNormal),
            "DetectionCombatToNormal" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.CombatToNormal),
            "DetectionCombatToLost" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.CombatToLost),
            "DetectionLostToNormal" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.LostToNormal),
            "DetectionLostToCombat" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.LostToCombat),
            "DetectFriendDie" => (DialogTopic.CategoryEnum.Detection, DialogTopic.SubtypeEnum.DetectFriendDie),
            "Custom" => (DialogTopic.CategoryEnum.Topic, DialogTopic.SubtypeEnum.Custom),
            _ => throw new NotSupportedException($"Category {text} not supported"),
        };
    }
}
