using _TeamFolder.JCJ.Battle.Session;

namespace _TeamFolder.JCJ.Script.Session
{
    /// <summary>
    /// JCJ 클라이언트가 모드별 게이트웨이에 접근하는 단일 진입점.
    /// 서버/네트워크 레이어는 이 허브만 구독·조회하면 되며, 구현 MonoBehaviour를 직접 Find 하지 않아도 된다.
    /// </summary>
    /// <remarks>
    /// 등록: 각 매니저 Awake에서 Register* / OnDestroy에서 Unregister* (내부).
    /// 조회: TryGet* 패턴 — null이면 아직 씬에 매니저가 없음.
    /// 배틀: <see cref="BattleMatchRegistry"/> 경유 (전역 싱글톤).
    /// </remarks>
    public static class JcjClientSessionHub
    {
        private static ITileRoundGateway s_tileRound;
        private static IMazeWorldGateway s_mazeWorld;
        private static IGameStateServerGateway s_gameState;
        private static IMatchScoreRankGateway s_scoreRank;

        /// <summary>배틀 매치 RPC·권한 적용용. <see cref="IBattleMatchGateway"/> 구현은 BattlePrototypeManager.</summary>
        public static bool TryGetBattle(out IBattleMatchGateway gateway) => BattleMatchRegistry.TryGetMatch(out gateway);

        /// <summary>배틀 데미지 팝업 등 프레젠테이션 설정.</summary>
        public static IBattlePopupPresentation BattlePopups => BattleMatchRegistry.Popups;

        /// <summary>타일(컬러 콜) 라운드 시작·리스폰·종료. 구현: TileGameManager.</summary>
        public static bool TryGetTileRound(out ITileRoundGateway gateway)
        {
            gateway = s_tileRound;
            return gateway != null;
        }

        /// <summary>미로 생성·좌표 변환·골 셀. 구현: MazeManager.</summary>
        public static bool TryGetMazeWorld(out IMazeWorldGateway gateway)
        {
            gateway = s_mazeWorld;
            return gateway != null;
        }

        /// <summary>미로 게임 상태(Waiting/Playing/Finished), 타이머, 카운트다운. 구현: GameStateManager.</summary>
        public static bool TryGetGameState(out IGameStateServerGateway gateway)
        {
            gateway = s_gameState;
            return gateway != null;
        }

        /// <summary>점수·점수 기준 등수. 구현: MatchScoreRankManager (완주/포디움은 미로 RankService).</summary>
        public static bool TryGetScoreRank(out IMatchScoreRankGateway gateway)
        {
            gateway = s_scoreRank;
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

        internal static void RegisterScoreRank(IMatchScoreRankGateway owner) => s_scoreRank = owner;

        internal static void UnregisterScoreRank(IMatchScoreRankGateway owner)
        {
            if (ReferenceEquals(s_scoreRank, owner)) s_scoreRank = null;
        }
    }
}
