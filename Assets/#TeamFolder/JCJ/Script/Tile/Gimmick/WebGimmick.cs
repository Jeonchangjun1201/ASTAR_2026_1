using System.Collections;
using UnityEngine;

// 플레이어 이동을 느리게 만드는 거미줄 기믹 처리.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 거미줄 기믹 (Purple 타일).
    /// 밟으면 webDuration 동안 플레이어 이동 제한.
    /// 이동 제한 종료 후 + stepDelay 없이 낙하.
    /// </summary>
    public class WebGimmick : MonoBehaviour, IGimmick
    {
        public bool FallsOnActivate => false;

        private GameConfig _config;

        public void Configure(GimmickContext ctx) => _config = ctx.Config;

        public void OnActivate(BaseTile tile, PlayerController player)
        {
            if (tile == null || player == null) return;
            StartCoroutine(WebRoutine(tile, player));
        }

        public void OnSubsequentStep(BaseTile tile, PlayerController player) { }

        private IEnumerator WebRoutine(BaseTile tile, PlayerController player)
        {
            if (tile == null) yield break;

            // 거미줄 색 시각 피드백
            tile.SetColor(new Color(0.45f, 0.22f, 0.55f));

            // 플레이어 이동 제한(그사이 폭탄 연쇄로 플레이어가 없어졌을 수 있음).
            if (player != null) player.ApplySlow(_config.webSpeedRatio, _config.webDuration);
            TileAudio.PlayStatic(TileSfx.Web, 0.8f, 0.9f);

            // 이동 제한 시간만큼 대기 후 낙하
            yield return new WaitForSeconds(_config.webDuration);
            // 대기 중 다른 기믹으로 타일이 파괴·강제 낙하했을 수 있음.
            if (tile != null) tile.StartFalling(skipPreDelay: true);
        }
    }
}
