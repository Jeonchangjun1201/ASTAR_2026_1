using System;
using System.Collections.Generic;

// 매니저가 참조하는 핵심 서비스 묶음 계약.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 게임 상태 변경을 알리고 외부에서 상태 전환을 요청할 수 있게 하는 서비스 계약.
    /// </summary>
    public interface IGameStateService
    {
        GameState CurrentState { get; }
        event Action<GameState> OnStateChanged;
        void ChangeState(GameState newState);
    }

    /// <summary>
    /// 라운드 제한 시간을 시작, 정지, 조정하고 남은 시간을 알리는 타이머 계약.
    /// </summary>
    public interface ITimerService
    {
        float Remaining { get; }
        event Action<float> OnTimerUpdated;
        event Action OnTimerExpired;
        void StartTimer(float duration);
        void StopTimer();
        void ResetTimer();
        /// <summary>진행 중인 타이머에 초를 더한다(음수면 감소).</summary>
        void AddTime(float seconds);
    }

    /// <summary>
    /// 플레이어 완주 순위와 최종 랭킹 확정을 담당하는 서비스 계약.
    /// 서버 권한 모드에서는 <see cref="FinishRequested"/>로 RPC를 보내고,
    /// 서버 확정 후 <see cref="ApplyAuthoritativeFinish"/>로 클라이언트 상태를 맞춘다.
    /// </summary>
    public interface IRankService
    {
        event Action<string, int> OnPlayerFinished;
        event Action<PlayerRankData> OnPlayerFinishedData;
        event Action<List<PlayerRankData>> OnAllFinished;
        /// <summary>서버 권한 모드: (playerId, displayName) 완주 요청. 네트워크 레이어가 구독한다.</summary>
        event Action<string, string> FinishRequested;

        void RegisterFinish(string playerName);
        void RegisterFinish(string playerId, string playerName);
        void ApplyAuthoritativeFinish(string playerId, string playerName);
        IReadOnlyList<PlayerRankData> GetRankings();
        void SetTotalPlayers(int total);
        void ResetRankings();
        /// <summary>
        /// 세션을 강제 종료한다 — 미배정 순위는 DNF로 채우고 <c>OnAllFinished</c>를 발생시킨다.
        /// 타이머 종료 시 <see cref="GameStateManager"/>에서 호출해 구현체와 관계없이 순위가 항상 확정되게 한다.
        /// </summary>
        void FinalizeNow();
    }

    /// <summary>
    /// 게임 시작 전 카운트다운 숫자와 GO 이벤트를 발생시키는 서비스 계약.
    /// </summary>
    public interface ICountdownService
    {
        event Action<int> OnTick;    // 3, 2, 1 틱
        event Action OnGo;           // GO!
        void Begin(int seconds);
        void Cancel();
    }

    /// <summary>
    /// 플레이어별 점수 누적과 점수 변경 알림을 제공하는 서비스 계약.
    /// 서버 권한 모드에서는 <see cref="ScoreChangeRequested"/>로 RPC를 보내고,
    /// 서버 확정 후 <see cref="ApplyAuthoritativeDelta"/>로 클라이언트 상태를 맞춘다.
    /// </summary>
    public interface IScoreService
    {
        event Action<string, int, int> OnScoreChanged;
        /// <summary>서버 권한 모드: (playerId, displayName, delta) 점수 변경 요청.</summary>
        event Action<string, string, int> ScoreChangeRequested;

        int GetScore(string playerName);
        void Add(string playerName, int delta);
        void Add(string playerId, string displayName, int delta);
        void ApplyAuthoritativeDelta(string playerId, string displayName, int delta);
        IReadOnlyList<PlayerRankData> GetRankings();
        int GetRankForPlayerIndex(int playerIndex);
        int GetRankByAliases(params string[] aliases);
        void Reset();
    }

    /// <summary>
    /// 포디움과 결과 HUD에서 사용하는 플레이어별 최종 순위 데이터.
    /// </summary>
    public struct PlayerRankData
    {
        public string PlayerId;
        public string PlayerName;
        public int    Rank;
        public int    Score;
    }
}
