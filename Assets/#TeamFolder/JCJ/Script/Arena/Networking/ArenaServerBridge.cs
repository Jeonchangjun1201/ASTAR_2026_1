using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script.Arena
{
    public class ArenaServerBridge : MonoBehaviour
    {
        public string BuildPreparationJson(string gameSessionId, string phaseId, ArenaModeType modeType, IReadOnlyList<ArenaPlayerSessionState> sessions)
        {
            var dto = new ArenaPrepPhaseDto
            {
                gameSessionId = gameSessionId,
                phaseId = phaseId,
                durationSec = ArenaDesignValues.PreparationDurationSeconds,
                modeType = (int)modeType,
                playerStates = BuildPlayerStates(sessions)
            };

            return JsonUtility.ToJson(dto);
        }

        public string BuildPurchaseRequestJson(string gameSessionId, string phaseId, string playerId, ArenaNodeId nodeId, int clientSequence)
        {
            var dto = new ArenaSkillPurchaseRequestDto
            {
                gameSessionId = gameSessionId,
                phaseId = phaseId,
                playerId = playerId,
                nodeId = nodeId.ToString(),
                clientSequence = clientSequence
            };

            return JsonUtility.ToJson(dto);
        }

        public string BuildSkipRequestJson(string gameSessionId, string phaseId, string playerId, bool isSkip)
        {
            var dto = new ArenaSkipReadyRequestDto
            {
                gameSessionId = gameSessionId,
                phaseId = phaseId,
                playerId = playerId,
                isSkip = isSkip
            };

            return JsonUtility.ToJson(dto);
        }

        public string BuildCombatActionJson(string gameSessionId, string playerId, ArenaCombatActionType actionType, string targetId, int chargeMs, Vector3 direction)
        {
            var dto = new ArenaCombatActionRequestDto
            {
                gameSessionId = gameSessionId,
                playerId = playerId,
                actionType = (int)actionType,
                targetId = targetId,
                chargeMs = chargeMs,
                directionX = direction.x,
                directionY = direction.y,
                directionZ = direction.z
            };

            return JsonUtility.ToJson(dto);
        }

        public string BuildResultJson(string gameSessionId, IReadOnlyList<ArenaPlayerSessionState> sessions)
        {
            var dto = new ArenaMinigameResultDto
            {
                gameSessionId = gameSessionId,
                placements = new List<ArenaPlacementDto>(),
                earnedScores = new List<ArenaEarnedScoreDto>()
            };

            for (int i = 0; i < sessions.Count; i++)
            {
                dto.placements.Add(new ArenaPlacementDto
                {
                    playerId = sessions[i].PlayerId,
                    placement = sessions[i].Placement
                });

                dto.earnedScores.Add(new ArenaEarnedScoreDto
                {
                    playerId = sessions[i].PlayerId,
                    earnedScore = ArenaGameManager.GetPlacementScoreStatic(sessions[i].Placement),
                    updatedStoredScore = sessions[i].StoredScore
                });
            }

            return JsonUtility.ToJson(dto);
        }

        private static List<ArenaPlayerSessionStateDto> BuildPlayerStates(IReadOnlyList<ArenaPlayerSessionState> sessions)
        {
            var results = new List<ArenaPlayerSessionStateDto>();
            for (int i = 0; i < sessions.Count; i++)
            {
                var dto = new ArenaPlayerSessionStateDto
                {
                    playerId = sessions[i].PlayerId,
                    displayName = sessions[i].DisplayName,
                    teamId = sessions[i].TeamId,
                    storedScore = sessions[i].StoredScore,
                    isReady = sessions[i].IsReady,
                    isAlive = sessions[i].IsAlive,
                    placement = sessions[i].Placement,
                    purchasedNodeIds = new List<string>()
                };

                for (int nodeIndex = 0; nodeIndex < sessions[i].PurchasedNodes.Count; nodeIndex++)
                {
                    dto.purchasedNodeIds.Add(sessions[i].PurchasedNodes[nodeIndex].ToString());
                }

                results.Add(dto);
            }

            return results;
        }
    }
}
