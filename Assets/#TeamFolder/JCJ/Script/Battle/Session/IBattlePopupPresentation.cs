namespace _TeamFolder.JCJ.Battle.Session
{
    // BattlePrototypeManager가 함께 구현한다. 데미지 팝업 스케일을 씬 밖 코드(BattleProjectile 등)에서 읽을 때 Popups로 접근한다.
    public interface IBattlePopupPresentation
    {
        float DamagePopupWorldScale { get; }
        int DamagePopupFontSize { get; }
        int DamagePopupHeadshotFontSize { get; }
    }
}
