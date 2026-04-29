using System.Collections.Generic;

namespace _TeamFolder.JCJ.Script.Arena
{
    public static class ArenaSkillCatalog
    {
        private static readonly List<ArenaSkillNodeDefinition> Definitions = Build();
        private static readonly Dictionary<ArenaNodeId, ArenaSkillNodeDefinition> ById = BuildMap();

        public static IReadOnlyList<ArenaSkillNodeDefinition> GetAll()
        {
            return Definitions;
        }

        public static ArenaSkillNodeDefinition Get(ArenaNodeId nodeId)
        {
            return ById[nodeId];
        }

        public static List<ArenaSkillNodeDefinition> GetByCategory(ArenaSkillCategory category)
        {
            var results = new List<ArenaSkillNodeDefinition>();
            for (int i = 0; i < Definitions.Count; i++)
            {
                if (Definitions[i].Category == category)
                {
                    results.Add(Definitions[i]);
                }
            }

            return results;
        }

        public static ArenaResolvedStats ResolveStats(IReadOnlyList<ArenaNodeId> purchasedNodes)
        {
            var stats = new ArenaResolvedStats();
            if (purchasedNodes == null)
            {
                return stats;
            }

            for (int i = 0; i < purchasedNodes.Count; i++)
            {
                var definition = Get(purchasedNodes[i]);
                for (int effectIndex = 0; effectIndex < definition.Effects.Count; effectIndex++)
                {
                    ApplyEffect(stats, definition.Effects[effectIndex]);
                }
            }

            return stats;
        }

        public static bool CanPurchase(ArenaPlayerSessionState session, ArenaNodeId nodeId, out string failureReason)
        {
            var definition = Get(nodeId);
            if (session.HasNode(nodeId))
            {
                failureReason = "이미 구매한 노드";
                return false;
            }

            if (session.StoredScore < definition.Cost)
            {
                failureReason = "점수 부족";
                return false;
            }

            for (int i = 0; i < definition.RequiredNodes.Count; i++)
            {
                if (!session.HasNode(definition.RequiredNodes[i]))
                {
                    failureReason = "선행 노드 필요";
                    return false;
                }
            }

            int tier1Count = 0;
            int tier2Count = 0;
            for (int i = 0; i < session.PurchasedNodes.Count; i++)
            {
                var purchased = Get(session.PurchasedNodes[i]);
                if (purchased.Category != definition.Category)
                {
                    continue;
                }

                if (purchased.Tier == ArenaNodeTier.Tier1)
                {
                    tier1Count++;
                }

                if (purchased.Tier == ArenaNodeTier.Tier2)
                {
                    tier2Count++;
                }
            }

            if (definition.Tier == ArenaNodeTier.Tier2 && tier1Count < 1)
            {
                failureReason = "같은 계열 1티어 1개 필요";
                return false;
            }

            if (definition.Tier == ArenaNodeTier.Tier3 && (tier1Count < 2 || tier2Count < 1))
            {
                failureReason = "같은 계열 1티어 2개와 2티어 1개 필요";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public static string BuildTooltip(ArenaSkillNodeDefinition definition)
        {
            return $"{definition.DisplayName}\n{definition.Description}\n비용: {definition.Cost}";
        }

        private static Dictionary<ArenaNodeId, ArenaSkillNodeDefinition> BuildMap()
        {
            var map = new Dictionary<ArenaNodeId, ArenaSkillNodeDefinition>();
            for (int i = 0; i < Definitions.Count; i++)
            {
                map[Definitions[i].NodeId] = Definitions[i];
            }

            return map;
        }

        private static void ApplyEffect(ArenaResolvedStats stats, ArenaSkillEffectDefinition effect)
        {
            switch (effect.EffectKind)
            {
                case ArenaNodeEffectKind.StrengthFlat:
                    stats.Strength += effect.IntValue;
                    break;
                case ArenaNodeEffectKind.KnockbackDealtMultiplier:
                    stats.KnockbackDealtMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.CarryMovePenaltyMultiplier:
                    stats.CarryMovePenaltyMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.ChargedPushPowerMultiplier:
                    stats.ChargedPushPowerMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.ThrowPowerMultiplier:
                    stats.ThrowPowerMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.MoveSpeedMultiplier:
                    stats.MoveSpeedMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.JumpForceMultiplier:
                    stats.JumpForceMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.AirControlMultiplier:
                    stats.AirControlMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.LandingBurstMultiplier:
                    stats.LandingBurstMultiplier += effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.AirDashUnlock:
                    stats.HasAirDash = true;
                    break;
                case ArenaNodeEffectKind.DoubleJumpUnlock:
                    stats.HasDoubleJump = true;
                    break;
                case ArenaNodeEffectKind.MaxHealthMultiplier:
                    stats.MaxHealthMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.KnockbackTakenMultiplier:
                    stats.KnockbackTakenMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.DamageTakenMultiplier:
                    stats.DamageTakenMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.ChargedKnockbackTakenMultiplier:
                    stats.ChargedKnockbackTakenMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.LastStandUnlock:
                    stats.HasLastStand = true;
                    break;
                case ArenaNodeEffectKind.AttackCooldownMultiplier:
                    stats.AttackCooldownMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.StaminaUseMultiplier:
                    stats.StaminaUseMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.ChargeTimeMultiplier:
                    stats.ChargeTimeMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.RecoveryTimeMultiplier:
                    stats.RecoveryTimeMultiplier *= effect.FloatValue;
                    break;
                case ArenaNodeEffectKind.StaminaRegenMultiplier:
                    stats.StaminaRegenMultiplier *= effect.FloatValue;
                    break;
            }
        }

        private static List<ArenaSkillNodeDefinition> Build()
        {
            var list = new List<ArenaSkillNodeDefinition>
            {
                Node(ArenaNodeId.StrengthTrainingI, ArenaSkillCategory.Strength, ArenaNodeTier.Tier1, "근력 단련 I", "힘 +1", ArenaDesignValues.Tier1Cost, Effect(ArenaNodeEffectKind.StrengthFlat, 0f, 1)),
                Node(ArenaNodeId.CarryHandling, ArenaSkillCategory.Strength, ArenaNodeTier.Tier2, "운반 숙련", "운반 중 이동 페널티 -15%", ArenaDesignValues.Tier2Cost, Effect(ArenaNodeEffectKind.CarryMovePenaltyMultiplier, 0.85f)),
                Node(ArenaNodeId.HeavyThrow, ArenaSkillCategory.Strength, ArenaNodeTier.Tier3, "강투 숙련", "투척 위력 +25%", ArenaDesignValues.Tier3Cost, Effect(ArenaNodeEffectKind.ThrowPowerMultiplier, 1.25f)),

                Node(ArenaNodeId.JumpBoostI, ArenaSkillCategory.Mobility, ArenaNodeTier.Tier1, "점프 증폭 I", "점프력 +10%", ArenaDesignValues.Tier1Cost, Effect(ArenaNodeEffectKind.JumpForceMultiplier, 1.10f)),
                Node(ArenaNodeId.AirControl, ArenaSkillCategory.Mobility, ArenaNodeTier.Tier2, "공중 제어", "공중 제어력 +20%", ArenaDesignValues.Tier2Cost, Effect(ArenaNodeEffectKind.AirControlMultiplier, 1.20f)),
                Node(ArenaNodeId.DoubleJump, ArenaSkillCategory.Mobility, ArenaNodeTier.Tier3, "이단 점프", "공중에서 추가 점프 가능", ArenaDesignValues.Tier3Cost, Effect(ArenaNodeEffectKind.DoubleJumpUnlock, 1f)),

                Node(ArenaNodeId.HealthBoostI, ArenaSkillCategory.Stability, ArenaNodeTier.Tier1, "체력 강화 I", "최대 체력 +10%", ArenaDesignValues.Tier1Cost, Effect(ArenaNodeEffectKind.MaxHealthMultiplier, 1.10f)),
                Node(ArenaNodeId.DamageReduction, ArenaSkillCategory.Stability, ArenaNodeTier.Tier2, "피해 완화", "받는 피해 -10%", ArenaDesignValues.Tier2Cost, Effect(ArenaNodeEffectKind.DamageTakenMultiplier, 0.90f)),
                Node(ArenaNodeId.LastStand, ArenaSkillCategory.Stability, ArenaNodeTier.Tier3, "위기 보정", "한 라운드 1회 치명상을 버팀", ArenaDesignValues.Tier3Cost, Effect(ArenaNodeEffectKind.LastStandUnlock, 1f)),

                Node(ArenaNodeId.TempoI, ArenaSkillCategory.Combat, ArenaNodeTier.Tier1, "공격 템포 I", "공격 재사용 대기 -10%", ArenaDesignValues.Tier1Cost, Effect(ArenaNodeEffectKind.AttackCooldownMultiplier, 0.90f)),
                Node(ArenaNodeId.ChargePrep, ArenaSkillCategory.Combat, ArenaNodeTier.Tier2, "강공 준비", "차징 준비 시간 -15%", ArenaDesignValues.Tier2Cost, Effect(ArenaNodeEffectKind.ChargeTimeMultiplier, 0.85f)),
                Node(ArenaNodeId.BreathII, ArenaSkillCategory.Combat, ArenaNodeTier.Tier3, "호흡 관리 II", "스태미나 회복속도 +20%", ArenaDesignValues.Tier3Cost, Effect(ArenaNodeEffectKind.StaminaRegenMultiplier, 1.20f))
            };
            Require(list, ArenaNodeId.CarryHandling, ArenaNodeId.StrengthTrainingI);
            Require(list, ArenaNodeId.HeavyThrow, ArenaNodeId.CarryHandling);
            Require(list, ArenaNodeId.AirControl, ArenaNodeId.JumpBoostI);
            Require(list, ArenaNodeId.DoubleJump, ArenaNodeId.AirControl);
            Require(list, ArenaNodeId.DamageReduction, ArenaNodeId.HealthBoostI);
            Require(list, ArenaNodeId.LastStand, ArenaNodeId.DamageReduction);
            Require(list, ArenaNodeId.ChargePrep, ArenaNodeId.TempoI);
            Require(list, ArenaNodeId.BreathII, ArenaNodeId.ChargePrep);
            return list;
        }

        private static ArenaSkillNodeDefinition Node(ArenaNodeId nodeId, ArenaSkillCategory category, ArenaNodeTier tier, string displayName, string description, int cost, params ArenaSkillEffectDefinition[] effects)
        {
            return new ArenaSkillNodeDefinition
            {
                NodeId = nodeId,
                Category = category,
                Tier = tier,
                DisplayName = displayName,
                Description = description,
                Cost = cost,
                RequiredNodes = new List<ArenaNodeId>(),
                Effects = new List<ArenaSkillEffectDefinition>(effects)
            };
        }

        private static ArenaSkillEffectDefinition Effect(ArenaNodeEffectKind effectKind, float floatValue, int intValue = 0)
        {
            return new ArenaSkillEffectDefinition(effectKind, floatValue, intValue);
        }

        private static void Require(List<ArenaSkillNodeDefinition> definitions, ArenaNodeId nodeId, ArenaNodeId requiredNodeId)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].NodeId == nodeId)
                {
                    definitions[i].RequiredNodes.Add(requiredNodeId);
                    return;
                }
            }
        }
    }
}
