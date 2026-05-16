using UnityEngine;

// 플레이어를 위로 띄우는 풍선 기믹 처리.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 풍선 기믹 (Orange 타일).
    /// 밟으면 플레이어를 balloonDuration 동안 낮게 띄움.
    /// 타일은 즉시 낙하 카운트다운 시작 (FallsOnActivate = true).
    /// </summary>
    public class BalloonGimmick : MonoBehaviour, IGimmick
    {
        public bool FallsOnActivate => true;

        private GameConfig _config;

        public void Configure(GimmickContext ctx) => _config = ctx.Config;

        public void OnActivate(BaseTile tile, PlayerController player)
        {
            player.ApplyBalloon(_config.balloonForce, _config.balloonDuration);
        }

        public void OnSubsequentStep(BaseTile tile, PlayerController player) { }
    }
}
