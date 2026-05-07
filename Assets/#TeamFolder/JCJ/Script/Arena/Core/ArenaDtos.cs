using System;
using System.Collections.Generic;

namespace _TeamFolder.JCJ.Script.Arena
{
    [Serializable]
    public sealed class ArenaPlayerSessionStateDto
    {
        public string playerId;
        public string displayName;
        public int teamId;
        public int storedScore;
        public bool isReady;
        public bool isAlive;
        public int placement;
        public List<string> purchasedNodeIds = new();
    }

    [Serializable]
    public sealed class ArenaPrepPhaseDto
    {
        public string gameSessionId;
        public string phaseId;
        public int durationSec;
        public int modeType;
        public List<ArenaPlayerSessionStateDto> playerStates = new();
    }

    [Serializable]
    public sealed class ArenaSkillPurchaseRequestDto
    {
        public string gameSessionId;
        public string phaseId;
        public string playerId;
        public string nodeId;
        public int clientSequence;
    }

    [Serializable]
    public sealed class ArenaSkillPurchaseConfirmedDto
    {
        public string playerId;
        public int remainingScore;
        public List<string> purchasedNodeIds = new();
        public List<string> appliedStatEntries = new();
    }

    [Serializable]
    public sealed class ArenaSkipReadyRequestDto
    {
        public string gameSessionId;
        public string phaseId;
        public string playerId;
        public bool isSkip;
    }

    [Serializable]
    public sealed class ArenaPlayerSpawnDto
    {
        public string playerId;
        public int teamId;
        public float positionX;
        public float positionY;
        public float positionZ;
    }

    [Serializable]
    public sealed class ArenaMinigameStartDto
    {
        public string gameSessionId;
        public string minigameId;
        public int modeType;
        public int durationSec;
        public List<ArenaPlayerSpawnDto> spawnEntries = new();
    }

    [Serializable]
    public sealed class ArenaCombatActionRequestDto
    {
        public string gameSessionId;
        public string playerId;
        public int actionType;
        public string targetId;
        public int chargeMs;
        public float directionX;
        public float directionY;
        public float directionZ;
    }

    [Serializable]
    public sealed class ArenaPlacementDto
    {
        public string playerId;
        public int placement;
    }

    [Serializable]
    public sealed class ArenaEarnedScoreDto
    {
        public string playerId;
        public int earnedScore;
        public int updatedStoredScore;
    }

    [Serializable]
    public sealed class ArenaMinigameResultDto
    {
        public string gameSessionId;
        public List<ArenaPlacementDto> placements = new();
        public List<ArenaEarnedScoreDto> earnedScores = new();
    }
}
