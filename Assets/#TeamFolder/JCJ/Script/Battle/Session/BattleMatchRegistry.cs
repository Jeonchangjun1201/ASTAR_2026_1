using _TeamFolder.JCJ.Battle;

namespace _TeamFolder.JCJ.Battle.Session
{
    // BattlePrototypeScene에 BattlePrototypeManager가 있을 때 자동 등록된다. 서버 클라이언트는 Match로 게이트웨이를 얻어 이벤트 구독·권한 메서드 호출을 한곳에 모을 수 있다.
    public static class BattleMatchRegistry
    {
        public const float DefaultDamagePopupWorldScale = 0.005f;
        public const int DefaultDamagePopupFontSize = 130;
        public const int DefaultDamagePopupHeadshotFontSize = 180;

        private static IBattleMatchGateway s_match;
        private static IBattlePopupPresentation s_popups;

        public static IBattleMatchGateway Match => s_match;
        public static IBattlePopupPresentation Popups => s_popups;

        public static bool TryGetMatch(out IBattleMatchGateway gateway)
        {
            gateway = s_match;
            return gateway != null;
        }

        internal static void Register(BattlePrototypeManager owner)
        {
            s_match = owner;
            s_popups = owner;
        }

        internal static void Unregister(BattlePrototypeManager owner)
        {
            if (ReferenceEquals(s_match, owner)) s_match = null;
            if (ReferenceEquals(s_popups, owner)) s_popups = null;
        }
    }
}
