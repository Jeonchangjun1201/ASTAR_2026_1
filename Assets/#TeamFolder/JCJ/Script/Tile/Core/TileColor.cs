namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// Normal(3) + Gimmick(6) = 9가지 타일 색상.
    /// 새 기믹 추가 시: 여기에 색상 추가 → TileFactory에 매핑만 하면 됨 (OCP).
    /// </summary>
    public enum TileColor
    {
        // ── Normal Tiles ─────────────────────────────
        Green,
        Blue,
        Yellow,

        // ── Gimmick Tiles ────────────────────────────
        Red,     // BombGimmick    : n초 후 주변 반경 폭발
        Purple,  // WebGimmick     : n초간 이동 제한 (거미줄)
        Cyan,    // IceGimmick     : 마찰력 0.05 미끄러운 타일
        Orange,  // BalloonGimmick : 1초 동안 낮게 부상
        Lime,    // TrampolineGimmick : 높이 튀어오름, N번 버팀
        Magenta  // ConfusionGimmick  : n초간 조작 반전
    }
}
