using System;
using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script.Arena
{
    public enum ArenaModeType
    {
        Team2v2 = 0,
        FreeForAll = 1
    }

    public enum ArenaPhase
    {
        Inactive = 0,
        Preparation = 1,
        Playing = 2,
        Finished = 3
    }

    public enum ArenaSkillCategory
    {
        Strength = 0,
        Mobility = 1,
        Stability = 2,
        Combat = 3
    }

    public enum ArenaNodeTier
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3
    }

    public enum ArenaNodeEffectKind
    {
        StrengthFlat = 0,
        KnockbackDealtMultiplier = 1,
        CarryMovePenaltyMultiplier = 2,
        ChargedPushPowerMultiplier = 3,
        ThrowPowerMultiplier = 4,
        MoveSpeedMultiplier = 5,
        JumpForceMultiplier = 6,
        AirControlMultiplier = 7,
        LandingBurstMultiplier = 8,
        AirDashUnlock = 9,
        DoubleJumpUnlock = 10,
        MaxHealthMultiplier = 11,
        KnockbackTakenMultiplier = 12,
        DamageTakenMultiplier = 13,
        ChargedKnockbackTakenMultiplier = 14,
        LastStandUnlock = 15,
        AttackCooldownMultiplier = 16,
        StaminaUseMultiplier = 17,
        ChargeTimeMultiplier = 18,
        RecoveryTimeMultiplier = 19,
        StaminaRegenMultiplier = 20
    }

    public enum ArenaNodeId
    {
        StrengthTrainingI = 0,
        PushBoostI = 1,
        CarryHandling = 2,
        ChargeImpact = 3,
        StrengthTrainingII = 4,
        HeavyThrow = 5,
        MoveBoostI = 6,
        JumpBoostI = 7,
        AirControl = 8,
        LandingBurst = 9,
        AirDash = 10,
        DoubleJump = 11,
        HealthBoostI = 12,
        KnockbackResistI = 13,
        DamageReduction = 14,
        BracePosture = 15,
        HealthBoostII = 16,
        LastStand = 17,
        TempoI = 18,
        BreathI = 19,
        ChargePrep = 20,
        Recovery = 21,
        TempoII = 22,
        BreathII = 23
    }

    public enum ArenaCombatActionType
    {
        Push = 0,
        ChargedPush = 1,
        PickUp = 2,
        Drop = 3,
        Throw = 4,
        Jump = 5,
        AirDash = 6
    }

    [Serializable]
    public struct ArenaPlayerSpawnEntry
    {
        public string PlayerId;
        public string DisplayName;
        public bool IsLocalControlled;
        public Color Tint;
        public int InitialScore;
    }

    [Serializable]
    public struct ArenaSkillEffectDefinition
    {
        public ArenaNodeEffectKind EffectKind;
        public float FloatValue;
        public int IntValue;

        public ArenaSkillEffectDefinition(ArenaNodeEffectKind effectKind, float floatValue, int intValue = 0)
        {
            EffectKind = effectKind;
            FloatValue = floatValue;
            IntValue = intValue;
        }
    }

    [Serializable]
    public sealed class ArenaSkillNodeDefinition
    {
        public ArenaNodeId NodeId;
        public ArenaSkillCategory Category;
        public ArenaNodeTier Tier;
        public string DisplayName;
        public string Description;
        public int Cost;
        public List<ArenaNodeId> RequiredNodes = new();
        public List<ArenaSkillEffectDefinition> Effects = new();
    }

    [Serializable]
    public sealed class ArenaResolvedStats
    {
        public int Strength = 1;
        public float MoveSpeedMultiplier = 1f;
        public float JumpForceMultiplier = 1f;
        public float AirControlMultiplier = 1f;
        public float LandingBurstMultiplier = 0f;
        public bool HasAirDash;
        public bool HasDoubleJump;
        public float MaxHealthMultiplier = 1f;
        public float KnockbackTakenMultiplier = 1f;
        public float DamageTakenMultiplier = 1f;
        public float ChargedKnockbackTakenMultiplier = 1f;
        public bool HasLastStand;
        public float AttackCooldownMultiplier = 1f;
        public float StaminaUseMultiplier = 1f;
        public float ChargeTimeMultiplier = 1f;
        public float RecoveryTimeMultiplier = 1f;
        public float StaminaRegenMultiplier = 1f;
        public float KnockbackDealtMultiplier = 1f;
        public float CarryMovePenaltyMultiplier = 1f;
        public float ChargedPushPowerMultiplier = 1f;
        public float ThrowPowerMultiplier = 1f;

        public ArenaResolvedStats Clone()
        {
            return new ArenaResolvedStats
            {
                Strength = Strength,
                MoveSpeedMultiplier = MoveSpeedMultiplier,
                JumpForceMultiplier = JumpForceMultiplier,
                AirControlMultiplier = AirControlMultiplier,
                LandingBurstMultiplier = LandingBurstMultiplier,
                HasAirDash = HasAirDash,
                HasDoubleJump = HasDoubleJump,
                MaxHealthMultiplier = MaxHealthMultiplier,
                KnockbackTakenMultiplier = KnockbackTakenMultiplier,
                DamageTakenMultiplier = DamageTakenMultiplier,
                ChargedKnockbackTakenMultiplier = ChargedKnockbackTakenMultiplier,
                HasLastStand = HasLastStand,
                AttackCooldownMultiplier = AttackCooldownMultiplier,
                StaminaUseMultiplier = StaminaUseMultiplier,
                ChargeTimeMultiplier = ChargeTimeMultiplier,
                RecoveryTimeMultiplier = RecoveryTimeMultiplier,
                StaminaRegenMultiplier = StaminaRegenMultiplier,
                KnockbackDealtMultiplier = KnockbackDealtMultiplier,
                CarryMovePenaltyMultiplier = CarryMovePenaltyMultiplier,
                ChargedPushPowerMultiplier = ChargedPushPowerMultiplier,
                ThrowPowerMultiplier = ThrowPowerMultiplier
            };
        }
    }

    [Serializable]
    public sealed class ArenaPlayerSessionState
    {
        public string PlayerId;
        public string DisplayName;
        public int TeamId;
        public int StoredScore;
        public bool IsReady;
        public bool IsAlive = true;
        public int Placement;
        public Color Tint;
        public List<ArenaNodeId> PurchasedNodes = new();
        public ArenaResolvedStats ResolvedStats = new();

        public bool HasNode(ArenaNodeId nodeId)
        {
            return PurchasedNodes.Contains(nodeId);
        }
    }

    public static class ArenaDesignValues
    {
        public const int PreparationDurationSeconds = 60;
        public const int RoundDurationSeconds = 120;
        public const int StandaloneSeedScore = 200;
        public const int TeamModeWeight = 2;
        public const int FfaModeWeight = 3;
        public const int FirstPlaceScore = 100;
        public const int SecondPlaceScore = 50;
        public const int ThirdPlaceScore = 10;
        public const int FourthPlaceScore = 0;
        public const int Tier1Cost = 30;
        public const int Tier2Cost = 60;
        public const int Tier3Cost = 100;
        public const float ArenaPlayableRadius = 15.5f;
        public const float StrengthPenaltyPickupMultiplier = 1.35f;
        public const float StrengthPenaltyCarryMoveMultiplier = 1.20f;
        public const float StrengthPenaltyThrowMultiplier = 0.75f;
        public const float StrengthNormalCarryMoveMultiplier = 1.10f;
        public const float StrengthOverPickupMultiplier = 0.75f;
        public const float StrengthOverThrowMultiplier = 1.20f;
    }
}
