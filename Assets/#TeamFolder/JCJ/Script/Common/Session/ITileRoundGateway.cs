using System;
using UnityEngine;
using _TeamFolder.JCJ.TileGame;

namespace _TeamFolder.JCJ.Script.Session
{
    public interface ITileRoundGateway
    {
        _TeamFolder.JCJ.TileGame.GameState State { get; }
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
