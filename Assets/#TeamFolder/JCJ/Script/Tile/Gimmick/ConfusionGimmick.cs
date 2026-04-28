using UnityEngine;

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 혼란 기믹 (Magenta 타일).
    /// 밟으면 confusionDuration 동안 WASD 방향이 반전됨.
    /// 타일은 즉시 낙하 카운트다운 (FallsOnActivate = true).
    /// </summary>
    public class ConfusionGimmick : MonoBehaviour, IGimmick
    {
        public bool FallsOnActivate => true;

        private GameConfig _config;

        public void Configure(GimmickContext ctx) => _config = ctx.Config;

        public void OnActivate(BaseTile tile, PlayerController player)
        {
            player.ApplyConfusion(_config.confusionDuration);
            TileAudio.PlayStatic(TileSfx.Confuse, 0.85f, 1.1f);
        }

        public void OnSubsequentStep(BaseTile tile, PlayerController player) { }
    }
}
