using UnityEngine;

// 플레이어를 튕겨 올리는 트램펄린 기믹 처리.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 트램폴린 기믹 (Lime 타일).
    /// 밟을 때마다 플레이어를 위로 발사. maxBounces 초과 시 낙하.
    /// FallsOnActivate = false → 바운스 횟수로 스스로 낙하 결정.
    /// </summary>
    public class TrampolineGimmick : MonoBehaviour, IGimmick
    {
        public bool FallsOnActivate => false;

        private GameConfig _config;
        private int        _bounceCount;

        public void Configure(GimmickContext ctx) => _config = ctx.Config;

        public void OnActivate(BaseTile tile, PlayerController player)
        {
            Bounce(tile, player);
        }

        public void OnSubsequentStep(BaseTile tile, PlayerController player)
        {
            Bounce(tile, player);
        }

        private void Bounce(BaseTile tile, PlayerController player)
        {
            _bounceCount++;
            player.ApplyLaunch(_config.trampolineForce);

            // 잔여 횟수 시각 피드백 (녹색 → 노란색 → 빨간색)
            float ratio = (float)_bounceCount / _config.trampolineMaxBounces;
            tile.SetColor(Color.Lerp(new Color(0.5f, 1f, 0.2f), Color.red, ratio));

            if (_bounceCount >= _config.trampolineMaxBounces)
                tile.StartFalling(skipPreDelay: true);
        }
    }
}
