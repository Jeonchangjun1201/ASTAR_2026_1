namespace PYH.MiniGame
{
    using Player;

    public interface IMiniGame
    {
        Player[] PlayerList { get; } // Players, Currently Playing a MiniGame
        int MaxPlayer { get; } // Maximum Players at The Start
        int CurrentPlayer { get; } // Uh, You know that Right?

        void Initialize();
        void GameEnd();
    }
}
