namespace _TeamFolder.JCJ.Battle.Session // 데미지 팝업 설정만 분리한 얇은 인터페이스다.
{
    public interface IBattlePopupPresentation // BattlePrototypeManager가 함께 구현해 레지스트리 Popups로 노출된다.
    {
        float DamagePopupWorldScale { get; } // 월드 공간 텍스트의 크기 배율이다.
        int DamagePopupFontSize { get; } // 일반 히트용 TMP 폰트 크기이다.
        int DamagePopupHeadshotFontSize { get; } // 헤드샷일 때 더 강조하기 위한 폰트 크기이다.
    }
}
