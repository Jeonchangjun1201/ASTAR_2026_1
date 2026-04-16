using System;
using System.Collections.Generic;

namespace _TeamFolder.JCJ.Script
{

    public interface IGameStateService
    {
        GameState CurrentState { get; }
        event Action<GameState> OnStateChanged;
        void ChangeState(GameState newState);
    }

    public interface ITimerService
    {
        float Remaining { get; }
        event Action<float> OnTimerUpdated;  // UI 갱신용
        event Action OnTimerExpired;         // 타임오버
        void StartTimer(float duration);
        void StopTimer();
    }

    public interface IRankService
    {
        event Action<string, int> OnPlayerFinished;              // 이름, 등수
        event Action<List<PlayerRankData>> OnAllFinished;        // 전원 완료
        void RegisterFinish(string playerName);
        IReadOnlyList<PlayerRankData> GetRankings();
    }
    public struct PlayerRankData
    {
        public string PlayerName;
        public int    Rank;
        public int    Score;
    }
}
