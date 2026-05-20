using System;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Session
{
    /// <summary>
    /// 미로 모드 게임 상태 + 하위 서비스(타이머·랭크·점수·카운트다운) 게이트웨이.
    /// 서버 권한: *Requested 이벤트로 RPC 전송 → 서버 확정 후 <see cref="ApplyAuthoritativeState"/> 호출.
    /// 구현: GameStateManager. 조회: JcjClientSessionHub.TryGetGameState.
    /// </summary>
    public interface IGameStateServerGateway : IGameStateService
    {
        ITimerService Timer { get; }
        IRankService Rank { get; }
        ICountdownService Countdown { get; }
        IScoreService Score { get; }

        /// <summary>클라이언트 StartGame() 요청 — 서버가 라운드 시작을 허용할 때 처리.</summary>
        event Action StartGameRequested;
        /// <summary>ResetToWaiting() 요청.</summary>
        event Action ResetRequested;
        /// <summary>ChangeState(newState) 요청.</summary>
        event Action<GameState> StateChangeRequested;

        void StartGame();
        void ResetToWaiting();
        void ApplyAuthoritativeState(GameState newState);
    }
}
