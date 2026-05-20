using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Battle.Session
{
    /// <summary>
    /// 배틀 매치 셋업·리스폰 게이트웨이. 구현: BattlePrototypeManager.
    /// 조회: JcjClientSessionHub.TryGetBattle / BattleMatchRegistry.
    /// 서버 권한: MatchSetupRequested·RespawnRequested 구독 → ApplyAuthoritative* 호출.
    /// </summary>
    public interface IBattleMatchGateway
    {
        event Action MatchSetupRequested;
        event Action<string> RespawnRequested;

        void ApplyAuthoritativeMatchSetup(int[] playerRanks, int[] playerTeamIndices);
        void ApplyAuthoritativeRespawn(string playerId, Vector3 respawnPosition);
        void StartMatchPresentation();
    }
}
