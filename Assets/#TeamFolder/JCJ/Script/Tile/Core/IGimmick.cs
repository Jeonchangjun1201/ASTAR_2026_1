

// 타일 기믹이 따라야 하는 동작 계약 인터페이스.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 기믹 인터페이스.
    /// FallsOnActivate  = true  → GimmickTile이 OnActivate 직후 StartFalling() 호출.
    /// FallsOnActivate  = false → 기믹이 적절한 시점에 tile.StartFalling(skipPreDelay) 직접 호출.
    /// </summary>
    public interface IGimmick
    {
        bool FallsOnActivate { get; }

        /// <summary>TileFactory에서 생성 직후 설정값 주입.</summary>
        void Configure(GimmickContext ctx);

        /// <summary>최초 밟힘 시 호출.</summary>
        void OnActivate(BaseTile tile, PlayerController player);

        /// <summary>두 번째 이후 밟힘 시 호출 (FallsOnActivate=false인 기믹만 의미 있음).</summary>
        void OnSubsequentStep(BaseTile tile, PlayerController player);
    }
}
