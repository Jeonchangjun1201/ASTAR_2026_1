using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 타일 생성 공장 (ITileFactory 구현체).
    /// 새 기믹 추가 시: TileColor 열거형 추가 → ColorMap/GimmickColors/CreateGimmick 만 수정 (OCP).
    /// </summary>
    public class TileFactory : ITileFactory
    {
        // ── 색상 → 렌더 색 ──────────────────────────
        private static readonly Dictionary<TileColor, Color> ColorMap = new()
        {
            { TileColor.Green,   new Color(0.22f, 0.88f, 0.52f) },
            { TileColor.Blue,    new Color(0.28f, 0.62f, 1.00f) },
            { TileColor.Yellow,  new Color(1.00f, 0.92f, 0.28f) },
            { TileColor.Red,     new Color(1.00f, 0.28f, 0.32f) },
            { TileColor.Purple,  new Color(0.72f, 0.35f, 0.98f) },
            { TileColor.Cyan,    new Color(0.45f, 0.95f, 1.00f) },
            { TileColor.Orange,  new Color(1.00f, 0.58f, 0.22f) },
            { TileColor.Lime,    new Color(0.62f, 1.00f, 0.35f) },
            { TileColor.Magenta, new Color(1.00f, 0.35f, 0.82f) },
        };

        private static readonly HashSet<TileColor> GimmickColors = new()
        {
            TileColor.Red, TileColor.Purple, TileColor.Cyan,
            TileColor.Orange, TileColor.Lime, TileColor.Magenta,
        };

        private static readonly HashSet<TileColor> WebPaceColors = new()
        {
            TileColor.Purple,
            TileColor.Magenta,
        };

        private readonly GameConfig    _config;
        private readonly TileBoard     _board;
        private readonly GimmickContext _ctx;

        public TileFactory(GameConfig config, TileBoard board)
        {
            _config = config;
            _board  = board;
            _ctx    = new GimmickContext(config, board);
        }

        public BaseTile CreateTile(TileColor color, Vector3 position,
                                   Transform parent, float fallDelayMultiplier = 1f)
        {
            // 모든 타일 오브젝트는 여기서 생성된다.
            // 서버 연동 시 tileId를 붙이기 가장 좋은 위치도 이 함수다.
            // 육각 프리즘 타일 — 정육각 상단 + 얇은 뚜껑(큐브 아님).
            GameObject go = new GameObject($"HexTile_{color}");
            go.transform.SetParent(parent);
            go.transform.position   = position;
            // 바닥 투영 폭 ≈ 2*radius(유닛 메시는 스케일 1일 때 반지름 1).
            // 실제 반지름은 TileBoard.hexRadius로 여기서 스케일 조정.
            go.transform.localScale = new Vector3(_config.hexRadius, _config.hexHeight, _config.hexRadius);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = HexMeshBuilder.GetShared();
            var mr = go.AddComponent<MeshRenderer>();

            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex     = true;

            bool isGimmickTile = GimmickColors.Contains(color);
            ApplyMaterial(go, ColorMap.GetValueOrDefault(color, Color.white), isGimmickTile, mr);

            // 층마다 fallDelayMultiplier가 다르다.
            // 아래층일수록 타일이 빨리 무너지는 식의 난이도 조절에 사용된다.
            float baseStepDelay = ResolveStepDelay(color);
            float stepDelay = baseStepDelay * fallDelayMultiplier;
            BaseTile tile;

            IGimmick gimmickImpl = isGimmickTile ? CreateGimmick(color, go) : null;
            if (isGimmickTile && gimmickImpl != null)
            {
                // 기믹 타일은 GimmickTile + IGimmick 구현체 조합으로 만든다.
                // 새 기믹은 TileColor, ColorMap, CreateGimmick에만 매핑하면 기존 구조를 크게 건드리지 않는다.
                GimmickTile gimmickTile = go.AddComponent<GimmickTile>();
                gimmickTile.Initialize(stepDelay, _config.warnDuration,
                                       _config.fallDuration, _config.fallDistance);
                gimmickTile.ConfigureFadeOut(_config.tileFadeOutEnabled, _config.tileFallShortDistance);

                gimmickImpl.Configure(_ctx);
                gimmickTile.SetGimmick(gimmickImpl);
                tile = gimmickTile;
            }
            else
            {
                if (isGimmickTile)
                {
                    Debug.LogWarning($"[TileFactory] No gimmick component for color {color}; falling back to NormalTile.");
                }
                NormalTile normalTile = go.AddComponent<NormalTile>();
                normalTile.Initialize(stepDelay, _config.warnDuration,
                                      _config.fallDuration, _config.fallDistance);
                normalTile.ConfigureFadeOut(_config.tileFadeOutEnabled, _config.tileFallShortDistance);
                tile = normalTile;
            }

            tile.SetColorTag(color);
            return tile;
        }

        private float ResolveStepDelay(TileColor color)
        {
            // 거미줄/혼란처럼 억울함이 생기기 쉬운 기믹은 기본 낙하 지연을 길게 잡는다.
            // 서버 담당자는 색상별 낙하 지연이 클라와 서버에서 동일해야 함을 주의해야 한다.
            if (WebPaceColors.Contains(color))
                return _config.stepDelayWeb > 0f ? _config.stepDelayWeb : _config.stepDelay;
            return _config.stepDelayDefault > 0f ? _config.stepDelayDefault : _config.stepDelay;
        }

        private static void ApplyMaterial(GameObject go, Color color, bool isGimmick, Renderer rend = null)
        {
            if (rend == null) rend = go.GetComponent<Renderer>();
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null)
                sh = Shader.Find("Standard");
            var mat = new Material(sh);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", isGimmick ? 0.22f : 0.08f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", isGimmick ? 0.72f : 0.48f);
            if (isGimmick && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.45f);
            }
            rend.material = mat;
        }

        /// <summary>색상에 맞는 기믹 컴포넌트를 go에 추가하고 반환.</summary>
        private static IGimmick CreateGimmick(TileColor color, GameObject go) =>
            color switch
            {
                TileColor.Red     => go.AddComponent<BombGimmick>(),
                TileColor.Purple  => go.AddComponent<WebGimmick>(),
                TileColor.Cyan    => go.AddComponent<IceGimmick>(),
                TileColor.Orange  => go.AddComponent<BalloonGimmick>(),
                TileColor.Lime    => go.AddComponent<TrampolineGimmick>(),
                TileColor.Magenta => go.AddComponent<ConfusionGimmick>(),
                _                 => null,
            };
    }
}
