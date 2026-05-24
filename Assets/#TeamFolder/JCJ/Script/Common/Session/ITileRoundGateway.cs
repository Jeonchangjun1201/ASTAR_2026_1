using System;
using UnityEngine;
using _TeamFolder.JCJ.TileGame;

namespace _TeamFolder.JCJ.Script.Session
{
    /// <summary>
    /// 타일(컬러 콜) 라운드 상태·리스폰 게이트웨이.
    /// 서버 권한: *Requested 이벤트 → 서버 확정 후 ApplyAuthoritative* 메서드 호출.
    /// 구현: TileGameManager. 조회: JcjClientSessionHub.TryGetTileRound.
    /// </summary>
    public interface ITileRoundGateway
    {
        GameState State { get; }
        GameConfig Config { get; }

        event Action RoundStartRequested;
        event Action RoundRestartRequested;
        event Action<string> RoundEndRequested;
        event Action<string> RespawnRequested;
        event Action<string> FallResolutionRequested;

        void BeginRound();
        void ApplyAuthoritativeRoundStart();
        void ApplyAuthoritativeRoundEnd(string cause);
        void RestartRound();
        void ApplyAuthoritativeRestart();
        void ApplyAuthoritativeRespawn(string playerId, Vector3 target, float invuln);
        Vector3 GetSafeRespawnPosition();
    }
}
