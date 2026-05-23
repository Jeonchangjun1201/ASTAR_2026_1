using System.Collections;
using UnityEngine;

// 폭발로 주변 타일과 플레이어에 영향을 주는 폭탄 기믹 처리.

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 폭탄 기믹 (Red 타일).
    /// 밟으면 bombDelay 후 같은 층 육각 그리드 bombHexRange 칸 내 타일을 강제 낙하.
    /// </summary>
    public class BombGimmick : MonoBehaviour, IGimmick
    {
        public bool FallsOnActivate => false; // 타이머 후 직접 StartFalling 호출

        private GameConfig _config;
        private TileBoard  _board;

        public void Configure(GimmickContext ctx)
        {
            _config = ctx.Config;
            _board  = ctx.Board;
        }

        public void OnActivate(BaseTile tile, PlayerController player)
        {
            if (tile == null) return;
            StartCoroutine(BombRoutine(tile));
        }

        public void OnSubsequentStep(BaseTile tile, PlayerController player) { }

        private IEnumerator BombRoutine(BaseTile tile)
        {
            if (tile == null) yield break;

            TileAudio.PlayStatic(TileSfx.BombTick, 0.6f, 0.8f);
            // 폭발 반경과 같은 크기의 예고 링.
            Vector3 center = tile.transform.position;
            float ringRadius = _board != null
                ? _board.GetHexRingWorldRadius(_config.bombHexRange)
                : _config.bombRadius;
            var ring = SpawnTelegraphRing(center, ringRadius, _config.bombDelay);

            yield return StartCoroutine(BlinkRed(tile, _config.bombDelay));

            if (ring != null) Object.Destroy(ring);

            TileAudio.PlayStatic(TileSfx.BombExplode, 1f, 0.95f);

            // 대기 중 밟던 타일이 다른 폭발로 사라졌을 수 있음 — 저장한 중심으로 반경 쿼리, 없으면 마지막 낙하 생략.
            if (_board != null)
            {
                var nearby = _board.GetTilesInHexRange(tile, _config.bombHexRange);
                for (int i = 0; i < nearby.Count; i++)
                {
                    if (nearby[i] != null)
                        nearby[i].StartFalling(skipPreDelay: true);
                }
            }
            if (tile != null) tile.StartFalling(skipPreDelay: true);
        }

        private static GameObject SpawnTelegraphRing(Vector3 center, float radius, float lifetime)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "BombRing";
            GameObject.Destroy(go.GetComponent<Collider>());
            go.transform.position   = center + new Vector3(0f, 0.13f, 0f);
            go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            var rend = go.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            var col = new Color(1f, 0.25f, 0.25f, 0.42f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            mat.color = col;
            if (mat.HasProperty("_Surface"))    mat.SetFloat("_Surface", 1f);     // 투명
            if (mat.HasProperty("_SrcBlend"))   mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))   mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))     mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            rend.material = mat;
            GameObject.Destroy(go, lifetime + 0.2f);
            return go;
        }

        private IEnumerator BlinkRed(BaseTile tile, float duration)
        {
            if (tile == null || !tile.TryGetComponent<Renderer>(out var rend))
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            Color original = rend.material.color;
            int   steps    = Mathf.Max(1, Mathf.RoundToInt(duration / 0.25f));
            float interval = duration / (steps * 2f);

            for (int i = 0; i < steps; i++)
            {
                // 연쇄 반응으로 타일/렌더러가 카운트다운 중 파괴될 수 있음.
                if (tile == null || rend == null) yield break;
                rend.material.color = Color.red;
                yield return new WaitForSeconds(interval);
                if (tile == null || rend == null) yield break;
                rend.material.color = original;
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
