using UnityEngine.Events;

namespace _TeamFolder.PYH._02.Scripts.MiniGame
{
    public interface IMiniGame
    {
        Player.Player[] PlayerList { get; } // Players, Currently Playing a MiniGame
        int MaxPlayer { get; } // Maximum Players at The Start
        int CurrentPlayer { get; } // Uh, You know that Right?
        UnityEvent OnMiniGameEndEvent { get; }
        
        void Initialize();
        void GameEnd();
    }
}
