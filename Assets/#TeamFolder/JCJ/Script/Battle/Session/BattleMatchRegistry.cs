using _TeamFolder.JCJ.Battle; // BattlePrototypeManager 타입을 Register 시그니처에서 참조한다.

namespace _TeamFolder.JCJ.Battle.Session // 전역 등록소는 배틀 세션 보조 유틸만 담는다.
{
    public static class BattleMatchRegistry // 정적 필드로 현재 씬의 게이트웨이를 한 번에 노출한다.
    {
        public const float DefaultDamagePopupWorldScale = 0.005f; // 레지스트리에 매니저가 없을 때 투사체가 쓰는 월드 스케일 폴백이다.
        public const int DefaultDamagePopupFontSize = 130; // 일반 히트 텍스트 크기 폴백이다.
        public const int DefaultDamagePopupHeadshotFontSize = 180; // 헤드샷일 때 더 크게 보이도록 하는 폴백이다.

        private static IBattleMatchGateway s_match; // 현재 등록된 매치 게이트웨이 참조이거나 null이다.
        private static IBattlePopupPresentation s_popups; // 동일 매니저가 팝업 설정도 구현하므로 함께 보관한다.

        public static IBattleMatchGateway Match => s_match; // 서버 코드가 이 프로퍼티만 읽어 이벤트 구독과 RPC 적용을 한다.
        public static IBattlePopupPresentation Popups => s_popups; // 데미지 숫자 UI 설정을 읽을 때 사용한다.

        public static bool TryGetMatch(out IBattleMatchGateway gateway) // null 체크를 한 번에 하고 싶을 때 Try 패턴을 제공한다.
        {
            gateway = s_match; // out 매개변수에 현재 참조를 넣는다.
            return gateway != null; // 등록 여부를 불리언으로 돌려준다.
        }

        internal static void Register(BattlePrototypeManager owner) // 매니저 Start에서 호출되어 전역 슬롯을 채운다.
        {
            s_match = owner; // 게이트웨이 구현체를 저장한다.
            s_popups = owner; // 동일 오브젝트가 팝업 설정 인터페이스도 만족한다.
        }

        internal static void Unregister(BattlePrototypeManager owner) // 매니저 파괴 시 대칭으로 호출된다.
        {
            if (ReferenceEquals(s_match, owner)) s_match = null; // 현재 등록과 동일할 때만 비운다.
            if (ReferenceEquals(s_popups, owner)) s_popups = null; // 팝업 슬롯도 같은 규칙으로 비운다.
        }
    }
}
