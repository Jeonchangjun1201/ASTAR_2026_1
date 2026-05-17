using System;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Session
{
    public interface IGameStateServerGateway : IGameStateService
    {
        ITimerService Timer { get; }
        IRankService Rank { get; }
        ICountdownService Countdown { get; }
        IScoreService Score { get; }
        event Action StartGameRequested;
        event Action ResetRequested;
        event Action<GameState> StateChangeRequested;
        void StartGame();
        void ResetToWaiting();
        void ApplyAuthoritativeState(GameState newState);
    }
}
