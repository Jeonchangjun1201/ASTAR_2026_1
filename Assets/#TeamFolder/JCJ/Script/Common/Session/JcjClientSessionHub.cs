using _TeamFolder.JCJ.Battle.Session;

namespace _TeamFolder.JCJ.Script.Session
{
    public static class JcjClientSessionHub
    {
        private static ITileRoundGateway s_tileRound;
        private static IMazeWorldGateway s_mazeWorld;
        private static IGameStateServerGateway s_gameState;

        public static bool TryGetBattle(out IBattleMatchGateway gateway) => BattleMatchRegistry.TryGetMatch(out gateway);

        public static IBattlePopupPresentation BattlePopups => BattleMatchRegistry.Popups;

        public static bool TryGetTileRound(out ITileRoundGateway gateway)
        {
            gateway = s_tileRound;
            return gateway != null;
        }

        public static bool TryGetMazeWorld(out IMazeWorldGateway gateway)
        {
            gateway = s_mazeWorld;
            return gateway != null;
        }

        public static bool TryGetGameState(out IGameStateServerGateway gateway)
        {
            gateway = s_gameState;
            return gateway != null;
        }

        internal static void RegisterTileRound(ITileRoundGateway owner) => s_tileRound = owner;

        internal static void UnregisterTileRound(ITileRoundGateway owner)
        {
            if (ReferenceEquals(s_tileRound, owner)) s_tileRound = null;
        }

        internal static void RegisterMazeWorld(IMazeWorldGateway owner) => s_mazeWorld = owner;

        internal static void UnregisterMazeWorld(IMazeWorldGateway owner)
        {
            if (ReferenceEquals(s_mazeWorld, owner)) s_mazeWorld = null;
        }

        internal static void RegisterGameState(IGameStateServerGateway owner) => s_gameState = owner;

        internal static void UnregisterGameState(IGameStateServerGateway owner)
        {
            if (ReferenceEquals(s_gameState, owner)) s_gameState = null;
        }
    }
}
